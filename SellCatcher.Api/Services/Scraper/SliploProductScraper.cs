using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProductScraper
{
    /// <summary>
    /// Web scraper for extracting product information from Silpo/ATB online catalog.
    /// Supports extraction of regular prices, sale prices, bulk pricing, and product metadata.
    /// Uses parallel page loading (3 pages at once)
    /// </summary>
    public class SilpoProductScraper : IProductScraper
    {
        private readonly ScraperConfig _config;
        private readonly string _category;

        /// <summary>
        /// Initializes a new instance of the SilpoProductScraper class.
        /// </summary>
        /// <param name="config">Configuration settings for the scraper behavior</param>
        /// <param name="category">Category name to assign to scraped products</param>
        public SilpoProductScraper(ScraperConfig config, string category)
        {
            _config = config ?? new ScraperConfig();
            _category = string.IsNullOrWhiteSpace(category) ? "Silpo" : category;
        }

        /// Main method that initializes browser context and starts parallel scraping
        public async Task<List<Product>> ScrapeAsync(List<string> catalogUrls)
        {
            var products = new List<Product>();

            using var playwright = await Playwright.CreateAsync();
            
            await using var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = _config.Headless,
                SlowMo = _config.SlowMo
            });

            Console.WriteLine($"Launching Firefox headless? {_config.Headless}");

            // Configure browser context with Ukrainian locale and anti-detection measures
            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true,
                JavaScriptEnabled = true,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:115.0) Gecko/20100101 Firefox/115.0",
                Locale = "uk-UA",
                ViewportSize = new ViewportSize { Width = 1366, Height = 768 },
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["Accept-Language"] = "uk-UA,uk;q=0.9",
                    ["Referer"] = "https://www.atbmarket.com/"
                }
            });
            // Disable webdriver fingerprint
            await context.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            ");
            // Start parallel scraping
            var result = await ScrapeCatalogsInParallelAsync(catalogUrls, context);

            await context.CloseAsync();
            return result;
        }
        /// Parses a single catalog page and extracts product card elements
        private async Task<List<Product>> ScrapeCatalogPageAsync(IPage page, string url)
        {
            var products = new List<Product>();
            // Product container selectors
            var selectors = new[]
            {
                "[class*='catalog-item__bottom']",
                ".product-tile[data-testid*='product']",
                ".product-card",
                "[class*='ProductTile']"
            };

            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90000 });
            // Find product card blocks
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
                    // Extract name
                    var nameEl = await el.QuerySelectorAsync(".product-card__title");
                    prod.Name = (await nameEl?.InnerTextAsync())?.Trim() ?? "";
                    // Price block
                    var priceContainer = await el.QuerySelectorAsync(".product-card-price");
                    if (priceContainer != null)
                    {
                        // Base price
                        var priceEl = await priceContainer.QuerySelectorAsync(".product-card-price__displayPrice");
                        if (priceEl != null)
                        {
                            var priceText = (await priceEl.InnerTextAsync())?.Trim() ?? "";
                            var m = Regex.Match(priceText, @"(\d+(?:[.,]\d+)?)");
                            if (m.Success) prod.Price = decimal.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                        }
                        // Bulk
                        var bulkPriceEl = await priceContainer.QuerySelectorAsync(".product-card-offer__price");
                        if (bulkPriceEl != null)
                        {
                            var bulkText = (await bulkPriceEl.InnerTextAsync())?.Trim() ?? "";
                            var m = Regex.Match(bulkText, @"(\d+(?:[.,]\d+)?)");
                            if (m.Success)
                            {
                                prod.BulkPrice = decimal.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                                prod.IsBulk = true;
                            }
                        }
                    }
                    // Old Price → Indicates sale
                    var oldPriceEl = await el.QuerySelectorAsync(".product-card-price__displayOldPrice");
                    if (oldPriceEl != null)
                    {
                        var oldPriceText = (await oldPriceEl.InnerTextAsync())?.Trim() ?? "";
                        var m = Regex.Match(oldPriceText, @"(\d+(?:[.,]\d+)?)");
                        if (m.Success)
                        {
                            prod.OldPrice = decimal.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                            prod.IsOnSale = prod.OldPrice > prod.Price;
                        }
                    }
                    // Discount text
                    var discountEl = await el.QuerySelectorAsync(".product-card-price__sale");
                    if (discountEl != null)
                        prod.Discount = (await discountEl.InnerTextAsync())?.Trim() ?? "";
                    // Weight 
                    var weightEl = await el.QuerySelectorAsync(".ft-typo-14-semibold span");
                    if (weightEl != null)
                    {
                        var w = (await weightEl.InnerTextAsync())?.Trim() ?? "";
                        prod.Name = $"{prod.Name} ({w})";
                    }

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
        /// Opens up to 3 catalog pages simultaneously
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