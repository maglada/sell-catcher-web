using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProductScraper
{
    /// <summary>
    /// Web scraper for extracting product information from Silpo online catalog.
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
            using var playwright = await Playwright.CreateAsync();
            
            await using var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = _config.Headless,
                SlowMo = _config.SlowMo
            });

            if (_config.EnableLogging)
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
                    ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
                    ["Accept-Language"] = "uk-UA,uk;q=0.9",
                    ["Referer"] = "https://silpo.ua/",
                    ["Cache-Control"] = "no-cache"
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
            
            // Main product card selector based on your HTML
            var productSelector = "article.product-card";

            try
            {
                await page.GotoAsync(url, new PageGotoOptions { 
                    WaitUntil = WaitUntilState.DOMContentLoaded, 
                    Timeout = 90000 
                });

                // Wait for product cards to load
                await page.WaitForSelectorAsync(productSelector, new PageWaitForSelectorOptions { Timeout = 30000 });

                var productElements = await page.QuerySelectorAllAsync(productSelector);
                
                if (_config.EnableLogging)
                    Console.WriteLine($"Found {productElements.Count} products on {url}");

                foreach (var el in productElements)
                {
                    try
                    {
                        var prod = new Product { Category = _category };

                        // Extract product name from h3.product-card__title
                        var nameEl = await el.QuerySelectorAsync("h3.product-card__title");
                        if (nameEl != null)
                        {
                            prod.Name = (await nameEl.InnerTextAsync())?.Trim() ?? "";
                        }

                        if (string.IsNullOrWhiteSpace(prod.Name))
                            continue;

                        // Extract weight/volume info
                        var weightEl = await el.QuerySelectorAsync(".ft-typo-14-semibold span, .ft-typo-16-semibold span");
                        if (weightEl != null)
                        {
                            var weight = (await weightEl.InnerTextAsync())?.Trim() ?? "";
                            if (!string.IsNullOrWhiteSpace(weight))
                                prod.Name = $"{prod.Name}, {weight}";
                        }

                        // Extract prices from .product-card-price container
                        var priceContainer = await el.QuerySelectorAsync(".product-card-price");
                        if (priceContainer != null)
                        {
                            // Current price (displayPrice)
                            var priceEl = await priceContainer.QuerySelectorAsync(".product-card-price__displayPrice");
                            if (priceEl != null)
                            {
                                var priceText = (await priceEl.InnerTextAsync())?.Trim() ?? "";
                                // Extract numeric value (handle format like "44.89 грн")
                                var m = Regex.Match(priceText, @"(\d+(?:[.,]\d+)?)");
                                if (m.Success)
                                {
                                    prod.Price = decimal.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                                }
                            }

                            // Old price (if on sale)
                            var oldPriceEl = await priceContainer.QuerySelectorAsync(".product-card-price__displayOldPrice");
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

                            // Discount percentage
                            var discountEl = await priceContainer.QuerySelectorAsync(".product-card-price__sale");
                            if (discountEl != null)
                            {
                                prod.Discount = (await discountEl.InnerTextAsync())?.Trim() ?? "";
                            }

                            // Bulk/offer price
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

                        // Extract image URL
                        var imgEl = await el.QuerySelectorAsync("a.product-card__link img.product-card__product-img");
                        if (imgEl != null)
                        {
                            var imgSrc = await imgEl.GetAttributeAsync("src") ?? 
                                        await imgEl.GetAttributeAsync("data-src") ?? "";
                            prod.ImageUrl = imgSrc;
                        }

                        // Extract rating (optional)
                        var ratingEl = await el.QuerySelectorAsync(".catalog-card-rating--value");
                        if (ratingEl != null)
                        {
                            var ratingText = (await ratingEl.InnerTextAsync())?.Trim() ?? "";
                            if (decimal.TryParse(ratingText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal rating))
                            {
                                // You can add a Rating property to Product class if needed
                            }
                        }

                        // Only add if we have essential data
                        if (!string.IsNullOrWhiteSpace(prod.Name) && prod.Price > 0)
                        {
                            products.Add(prod);

                            if (_config.EnableLogging)
                            {
                                Console.WriteLine($"Added: {prod.Name} — {prod.Price}грн");
                                
                                if (!string.IsNullOrEmpty(prod.ImageUrl))
                                    Console.WriteLine($"  Image: {prod.ImageUrl}");
                                
                                if (prod.IsBulk)
                                    Console.WriteLine($"  BULK: {prod.BulkPrice}грн");
                                
                                if (prod.IsOnSale)
                                    Console.WriteLine($"  SALE: {prod.OldPrice}грн → {prod.Price}грн ({prod.Discount})");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_config.EnableLogging)
                            Console.WriteLine($"Error parsing product card: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scraping page {url}: {ex.Message}");
                
                if (_config.SaveErrorScreenshots)
                {
                    try
                    {
                        await page.ScreenshotAsync(new PageScreenshotOptions 
                        { 
                            Path = $"error_{DateTime.Now:yyyyMMddHHmmss}.png", 
                            FullPage = true 
                        });
                    }
                    catch { }
                }
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