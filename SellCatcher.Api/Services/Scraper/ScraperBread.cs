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
            var result = await ScrapeCatalogsInParallelAsync(catalogUrls, context);

            await context.CloseAsync();
            return result;
        }

        /// <summary>
        /// Scrapes a single catalog page and extracts all products
        /// </summary>
        private async Task<List<Product>> ScrapeCatalogPageAsync(IPage page, string url)
        {
            var products = new List<Product>();
            // Product container selectors
            var selectors = new[]
            {
                "[data-testid*='product-tile'], .ProductTile, .ProductTileLink"
            };

            await page.GotoAsync(url);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // Fetch the full HTML for debugging/inspection
            var html = await page.ContentAsync();
            if (_config.EnableLogging)
                Console.WriteLine($"Page HTML length: {html.Length}");

            // Save initial screenshot for debugging
            if (_config.SaveDebugScreenshots)
                await page.ScreenshotAsync(new() { Path = $"debug_{DateTime.Now:yyyyMMddHHmmss}.png", FullPage = true });

            IElementHandle[] els = Array.Empty<IElementHandle>();
            foreach (var sel in selectors)
            {
                var items = await page.QuerySelectorAllAsync(sel);
                if (items.Count > 0) { els = items.ToArray(); break; }
            }

            foreach (var el in els)
            {
                try
                {
                    var prod = new Product { Category = _category };

                    /// Extract name
                    var nameSel = await el.QuerySelectorAsync("[data-testid='product_tile_title']");
                    if (nameSel != null)
                        prod.Name = (await nameSel.InnerTextAsync())?.Trim() ?? "";
                    else
                        continue;

                    /// Extract price
                    var priceSel = await el.QuerySelectorAsync("[data-marker='Discounted Price'] span.Price__value_body") ??
                                  await el.QuerySelectorAsync(".Price__value_caption") ??
                                  await el.QuerySelectorAsync(".Price__value_unavailable") ??
                                  await el.QuerySelectorAsync(".Price__value_discount");
                    if (priceSel != null)
                    {
                        var priceText = (await priceSel.InnerTextAsync())?.Trim() ?? "";
                        var m = Regex.Match(priceText, @"(\d+(?:[.,]\d+)?)");
                        if (m.Success) prod.Price = decimal.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                    }

                    /// Extract old price
                    var oldPriceSel = await el.QuerySelectorAsync("[data-marker='Old Price'] span.Price__value_body") ??
                                     await el.QuerySelectorAsync(".ProductTile_oldPrice span") ??
                                     await el.QuerySelectorAsync(".Price__value_old");
                    if (oldPriceSel != null)
                    {
                        var oldPriceText = (await oldPriceSel.InnerTextAsync())?.Trim() ?? "";
                        var m = Regex.Match(oldPriceText, @"(\d+(?:[.,]\d+)?)");
                        if (m.Success)
                        {
                            prod.OldPrice = decimal.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                            prod.IsOnSale = prod.OldPrice > prod.Price;
                        }
                    }

                    /// Extract percent of discount
                    var discountSel = await el.QuerySelectorAsync("[data-marker='Discount']") ??
                                     await el.QuerySelectorAsync(".DiscountBadge") ??
                                     await el.QuerySelectorAsync("[class*='discount']") ??
                                     await el.QuerySelectorAsync("[class*='Discount']");
                    if (discountSel != null)
                    {
                        var discountText = (await discountSel.InnerTextAsync())?.Trim() ?? "";
                        var match = Regex.Match(discountText, @"[+\-]?\d+\s*%");
                        if (match.Success)
                            prod.Discount = match.Value.Trim();
                    }

                    /// Extract bulk price
                    var bulkPriceSel = await el.QuerySelectorAsync("[data-marker^='PriceWholesale_']");
                    if (bulkPriceSel != null)
                    {
                        var bulkText = (await bulkPriceSel.InnerTextAsync())?.Trim() ?? "";

                        var match = Regex.Match(bulkText, @"([\d.,]+)\s*₴");
                        if (match.Success)
                        {
                            var priceStr = match.Groups[1].Value.Replace(',', '.');
                            prod.BulkPrice = decimal.Parse(priceStr, CultureInfo.InvariantCulture);
                            prod.IsBulk = true;
                        }
                    }

                    /// Extract discount period
                    string validUntil = "";

                    var validUntilSel = await el.QuerySelectorAsync("[data-marker='Promotion_until_date']");
                    if (validUntilSel != null)
                    {
                        validUntil = (await validUntilSel.InnerTextAsync())?
                            .Replace("до", "")
                            .Trim() ?? "";
                    }
                    if (string.IsNullOrWhiteSpace(prod.Discount))
                        validUntil = "";

                    DateTime? trueValidUntil = null;

                    if (DateTime.TryParseExact(validUntil, "dd.MM", CultureInfo.InvariantCulture,
                                               DateTimeStyles.None, out var dt))
                    {
                        dt = new DateTime(DateTime.Now.Year, dt.Month, dt.Day);

                        if (dt < DateTime.Now)
                            dt = dt.AddYears(1);

                        trueValidUntil = dt;
                    }

                    prod.ValidUntil = trueValidUntil;

                    if (!string.IsNullOrWhiteSpace(prod.Name))
                    {
                        products.Add(prod);

                        Console.WriteLine($"Added: {prod.Name} — {prod.Price}grn");

                        if (prod.IsBulk)
                            Console.WriteLine($"It`s a BULK: {prod.BulkPrice}grn");

                        if (prod.IsOnSale)
                            Console.WriteLine($"It`s a SALE: {prod.OldPrice}grn → {prod.Price}grn ({prod.Discount})");
                    }
                }
                catch { }
            }
            return products;
        }

        /// <summary>
        /// Opens each URL in a new Playwright page, collects product data from all pages,
        /// and returns a combined list of all extracted products.
        /// </summary>
        /// <param name="catalogUrls">A list of catalog page URLs to scrape</param>
        /// <param name="context">An existing Playwright browser context used to create new pages</param>
        /// <returns>Сombined list of objects extracted from all provided URLs</returns>
        private async Task<List<Product>> ScrapeCatalogsInParallelAsync(List<string> urls, IBrowserContext context)
        {
            var all = new List<Product>();
            var semaphore = new SemaphoreSlim(3);

            var tasks = urls.Select(async url =>
            {
                if (string.IsNullOrWhiteSpace(url)) return;

                await semaphore.WaitAsync();
                try
                {
                    var page = await context.NewPageAsync();
                    var items = await ScrapeCatalogPageAsync(page, url);
                    await page.CloseAsync();

                    lock (all) all.AddRange(items);
                }
                finally { semaphore.Release(); }
            });

            await Task.WhenAll(tasks);
            return all;
        }
    }
}
        