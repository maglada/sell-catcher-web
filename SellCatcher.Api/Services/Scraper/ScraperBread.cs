using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProductScraper
{
    public class ProductScraperBread : IProductScraper
    {
        private readonly ScraperConfig _config;
        private readonly string _category;

        public ProductScraperBread(ScraperConfig config = null, string category = null)
        {
            _config = config ?? new ScraperConfig();
            _category = category ?? "Unknown";
        }

        /// <summary>
        /// Main scraping method that processes multiple catalog URLs and returns extracted products
        /// </summary>
        public async Task<List<Product>> ScrapeAsync(List<string> catalogUrls)
        {
            var products = new List<Product>();

            // Create Playwright instance
            using var playwright = await Playwright.CreateAsync();

            // Launch Chromium in headless mode (with configurable slowdown to debug interactions)
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = _config.Headless,
                SlowMo = _config.SlowMo
            });

            // Configure browser context with realistic headers & settings
            var context = await browser.NewContextAsync(new()
            {
                IgnoreHTTPSErrors = true,
                JavaScriptEnabled = true,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/116.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                Locale = "uk-UA",
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
                    ["Accept-Language"] = "uk-UA,uk;q=0.9,en-US;q=0.8,en;q=0.7",
                    ["Cache-Control"] = "no-cache",
                    ["Pragma"] = "no-cache"
                }
            });

            var page = await context.NewPageAsync();

            // Hook console & page error messages from the browser
            if (_config.EnableLogging)
            {
                page.Console += (_, msg) => Console.WriteLine($"BROWSER: {msg.Text}");
                page.PageError += (_, err) => Console.WriteLine($"PAGE ERROR: {err}");
            }

            /// Start parsing for all links
            products = await ScrapeCatalogsInParallelAsync(catalogUrls, context);
            await context.CloseAsync();
            return products;
        }

        /// <summary>
        /// Scrapes a single catalog page and extracts all products
        /// </summary>
        private async Task<List<Product>> ScrapeCatalogPageAsync(IPage page, string catalogUrl)
        {
            var products = new List<Product>();

            await page.GotoAsync(catalogUrl);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // Fetch the full HTML for debugging/inspection
            var html = await page.ContentAsync();
            if (_config.EnableLogging)
                Console.WriteLine($"Page HTML length: {html.Length}");

            // Save initial screenshot for debugging
            if (_config.SaveDebugScreenshots)
                await page.ScreenshotAsync(new() { Path = $"debug_{DateTime.Now:yyyyMMddHHmmss}.png", FullPage = true });

            // Try to find product elements by multiple selector variations
            var productElements = await page.QuerySelectorAllAsync(
                "[data-testid*='product-tile'], .ProductTile, .ProductTileLink"
            );

            /// Processing each product
            var tasks = productElements.Select(async product =>
            {
                try
                {
                    /// Extract name
                    var nameSel = await product.QuerySelectorAsync("[data-testid='product_tile_title']");
                    var name = nameSel != null ? (await nameSel.InnerTextAsync())?.Trim() : "";

                    /// Extract price
                    var priceSel = await product.QuerySelectorAsync("[data-marker='Discounted Price'] span.Price__value_body") ??
                                  await product.QuerySelectorAsync(".Price__value_caption") ??
                                  await product.QuerySelectorAsync(".Price__value_unavailable") ??
                                  await product.QuerySelectorAsync(".Price__value_discount");
                    var newPrice = priceSel != null ? (await priceSel.InnerTextAsync())?.Trim() : "";

                    /// Extract old price
                    var oldPriceSel = await product.QuerySelectorAsync("[data-marker='Old Price'] span.Price__value_body") ??
                                     await product.QuerySelectorAsync(".ProductTile_oldPrice span") ??
                                     await product.QuerySelectorAsync(".Price__value_old");
                    var nonDiscountPrice = oldPriceSel != null ? (await oldPriceSel.InnerTextAsync())?.Trim() : "";

                    /// Extract percent of discount
                    var discountSel = await product.QuerySelectorAsync("[data-marker='Discount']") ??
                                     await product.QuerySelectorAsync(".DiscountBadge") ??
                                     await product.QuerySelectorAsync("[class*='discount']") ??
                                     await product.QuerySelectorAsync("[class*='Discount']");

                    string discount = "";
                    if (discountSel != null)
                    {
                        var discountText = (await discountSel.InnerTextAsync())?.Trim() ?? "";
                        var match = Regex.Match(discountText, @"[+\-]?\d+\s*%");
                        if (match.Success)
                            discount = match.Value.Trim();
                    }

                    /// Extract discount period
                    string validUntil = "";
                    var validUntilSel = await product.QuerySelectorAsync("[data-marker='Promotion_until_date']");
                    if (validUntilSel != null)
                        validUntil = (await validUntilSel.InnerTextAsync())?.Replace("до", "").Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(discount))
                        nonDiscountPrice = "";

                    /// Removing blanks, punctuation marks and other rubbish
                    string CleanPrice(string p) => p.Replace(" ", "").Replace(",", ".").Replace("₴", "").Trim();

                    var cleanedNewPrice = CleanPrice(newPrice);
                    var cleanedOldPrice = CleanPrice(nonDiscountPrice);

                    if (!decimal.TryParse(cleanedNewPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price))
                        return;

                    decimal? oldPrice = null;
                    if (decimal.TryParse(cleanedOldPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal priceOld))
                        oldPrice = priceOld;

                    /// Convert string to date
                    DateTime? trueValidUntil = null;
                    if (DateTime.TryParseExact(validUntil, "dd.MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    {
                        dt = new DateTime(DateTime.Now.Year, dt.Month, dt.Day);
                        if (dt < DateTime.Now)
                            dt = dt.AddYears(1);
                        trueValidUntil = dt;
                    }

                    /// Object formation
                    var p = new Product
                    {
                        Name = name,
                        Price = price,
                        OldPrice = oldPrice,
                        Discount = discount,
                        IsOnSale = !string.IsNullOrEmpty(discount) || oldPrice.HasValue,
                        ValidUntil = trueValidUntil,
                        Category = _category
                    };

                    /// Add to public list in parallel mode
                    lock (products)
                        products.Add(p);

                    Console.WriteLine($"Added: {p.Name} — {p.Price}₴ {(p.IsOnSale ? "(ALARM! It`s SALE)" : "")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to add product: {ex.Message}");
                }
            });
            await Task.WhenAll(tasks);
            Console.WriteLine($"Total products added: {products.Count}");
            return products;
        }

        /// <summary>
        /// Opens each URL in a new Playwright page, collects product data from all pages,
        /// and returns a combined list of all extracted products.
        /// </summary>
        /// <param name="catalogUrls">A list of catalog page URLs to scrape</param>
        /// <param name="context">An existing Playwright browser context used to create new pages</param>
        /// <returns>Сombined list of objects extracted from all provided URLs</returns>
        private async Task<List<Product>> ScrapeCatalogsInParallelAsync(List<string> catalogUrls, IBrowserContext context)
        {
            var allProducts = new List<Product>();
            var semaphore = new SemaphoreSlim(3);

            /// Parallel processing
            var tasks = catalogUrls.Select(async url =>
            {
                if (string.IsNullOrWhiteSpace(url)) return;

                /// Wrapper of semaphore
                await semaphore.WaitAsync();
                try
                {
                    Console.WriteLine($"Parsing: {url}");
                    var page = await context.NewPageAsync();
                    var pageProducts = await ScrapeCatalogPageAsync(page, url);
                    await page.CloseAsync();

                    lock (allProducts)
                        allProducts.AddRange(pageProducts);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{url}: {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return allProducts;
        }
    }
}
        