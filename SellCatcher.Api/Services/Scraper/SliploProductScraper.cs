using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;

namespace ProductScraper
{
    /// <summary>
    /// Web scraper for extracting product information from Silpo/ATB online catalog.
    /// Supports extraction of regular prices, sale prices, bulk pricing, and product metadata.
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

        /// <summary>
        /// Scrapes product information from the provided catalog URLs.
        /// </summary>
        /// <param name="catalogUrls">List of URLs to scrape products from</param>
        /// <returns>A list of Product objects containing scraped data</returns>
        /// <remarks>
        /// This method:
        /// - Uses Firefox browser with anti-detection measures
        /// - Implements random delays between requests to avoid rate limiting
        /// - Extracts product names, prices, bulk pricing, discounts, and metadata
        /// - Takes error screenshots when configured to do so
        /// </remarks>
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

            var page = await context.NewPageAsync();

            // Setup logging if enabled
            if (_config.EnableLogging)
            {
                page.Console += (_, msg) => Console.WriteLine($"BROWSER: {msg.Text}");
                page.PageError += (_, err) => Console.WriteLine($"PAGE ERROR: {err}");
            }

            // Inject anti-detection script
            await page.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                delete navigator.__proto__.webdriver;
            ");

            // Define possible selectors for product elements
            var selectors = new[]
            {
                "[class*='catalog-item__bottom']",
                ".product-tile[data-testid*='product']",
                ".product-card",
                "[class*='ProductTile']"
            };

            // Iterate through each catalog URL
            for (int i = 0; i < catalogUrls.Count; i++)
            {
                var url = catalogUrls[i];
                if (string.IsNullOrWhiteSpace(url)) continue;

                try
                {
                    if (_config.EnableLogging) Console.WriteLine($"\n[{i+1}/{catalogUrls.Count}] ATB: navigating to {url}");

                    // Random delay before request to mimic human behavior
                    var delayMs = new Random().Next(5000, 10000);
                    if (_config.EnableLogging) Console.WriteLine($"Pre-request delay: {delayMs}ms");
                    await Task.Delay(delayMs);

                    page.SetDefaultNavigationTimeout(90000);
                    
                    var response = await page.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 90000
                    });
                    
                    Console.WriteLine($"Response status: {response?.Status}");

                    // Try to find products using different selectors
                    IElementHandle[] els = Array.Empty<IElementHandle>();
                    foreach (var sel in selectors)
                    {
                        try
                        {
                            var maybe = await page.QuerySelectorAllAsync(sel);
                            if (maybe != null && maybe.Count > 0)
                            {
                                els = maybe.ToArray();
                                if (_config.EnableLogging) Console.WriteLine($"Found {els.Length} elements with selector: {sel}");
                                break;
                            }
                        }
                        catch { }
                    }

                    if (els.Length == 0)
                    {
                        Console.WriteLine($"No product elements found on {url}");
                        
                        if (_config.EnableLogging)
                        {
                            var bodyText = await page.Locator("body").InnerTextAsync();
                            Console.WriteLine($"Page preview: {bodyText.Substring(0, Math.Min(200, bodyText.Length))}...");
                        }
                    }
                    else
                    {
                        int beforeCount = products.Count;
                        
                        // Parse each product element
                        foreach (var el in els)
                        {
                            try
                            {
                                var prod = new Product { Category = _category };

                                // Extract product name
                                var nameEl = await el.QuerySelectorAsync(".product-card__title");
                                prod.Name = (await nameEl?.InnerTextAsync())?.Trim() ?? "";

                                // Get the price container which holds both retail and bulk pricing
                                var priceContainer = await el.QuerySelectorAsync(".product-card-price");
                                
                                if (priceContainer != null)
                                {
                                    // Extract current retail price
                                    var priceEl = await priceContainer.QuerySelectorAsync(".product-card-price__displayPrice");
                                    if (priceEl != null)
                                    {
                                        var priceText = (await priceEl.InnerTextAsync())?.Trim() ?? "";
                                        var price = Regex.Match(priceText, @"(\d+(?:[.,]\d+)?)");
                                        if (price.Success && decimal.TryParse(price.Groups[1].Value.Replace(',', '.'),
                                            NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var p))
                                        {
                                            prod.Price = p;
                                        }
                                    }

                                    // Check for bulk pricing offer
                                    var bulkPriceEl = await priceContainer.QuerySelectorAsync(".product-card-offer__price");
                                    if (bulkPriceEl != null)
                                    {
                                        var bulkPriceText = (await bulkPriceEl.InnerTextAsync())?.Trim() ?? "";
                                        var bulkPrice = Regex.Match(bulkPriceText, @"(\d+(?:[.,]\d+)?)");
                                        if (bulkPrice.Success && decimal.TryParse(bulkPrice.Groups[1].Value.Replace(',', '.'),
                                            NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var bp))
                                        {
                                            prod.BulkPrice = bp;
                                            prod.IsBulk = true;
                                            
                                            // Log bulk quantity requirement if available (e.g., "від 2 шт")
                                            var bulkValueEl = await priceContainer.QuerySelectorAsync(".product-card-offer__value");
                                            if (bulkValueEl != null && _config.EnableLogging)
                                            {
                                                var bulkValueText = (await bulkValueEl.InnerTextAsync())?.Trim() ?? "";
                                                Console.WriteLine($"  Bulk offer: {bulkValueText} at {bp} грн");
                                            }
                                        }
                                    }
                                }

                                // Extract old/original price (for sale items)
                                var oldPriceEl = await el.QuerySelectorAsync(".product-card-price__displayOldPrice");
                                if (oldPriceEl != null)
                                {
                                    var oldPriceText = (await oldPriceEl.InnerTextAsync())?.Trim() ?? "";
                                    var oldPrice = Regex.Match(oldPriceText, @"(\d+(?:[.,]\d+)?)");
                                    if (oldPrice.Success && decimal.TryParse(oldPrice.Groups[1].Value.Replace(',', '.'),
                                        NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var op))
                                    {
                                        prod.OldPrice = op;
                                        prod.IsOnSale = op > prod.Price;
                                    }
                                }

                                // Extract discount percentage or label
                                var discountEl = await el.QuerySelectorAsync(".product-card-price__sale");
                                if (discountEl != null)
                                {
                                    prod.Discount = (await discountEl.InnerTextAsync())?.Trim() ?? "";
                                }

                                // Extract weight/amount information
                                var weightEl = await el.QuerySelectorAsync(".ft-typo-14-semibold span");
                                if (weightEl != null)
                                {
                                    var weightText = (await weightEl.InnerTextAsync())?.Trim() ?? "";
                                    prod.Name = $"{prod.Name} ({weightText})";
                                }

                                // Add product if it has a valid name
                                if (!string.IsNullOrWhiteSpace(prod.Name))
                                {
                                    products.Add(prod);
                                    
                                    if (_config.EnableLogging && prod.IsBulk)
                                    {
                                        Console.WriteLine($"  ✓ Found bulk product: {prod.Name} - Retail: {prod.Price} грн, Bulk: {prod.BulkPrice} грн");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                if (_config.EnableLogging) Console.WriteLine($"Error parsing product: {ex.Message}");
                            }
                        }

                        // Report extraction statistics for current page
                        int addedCount = products.Count - beforeCount;
                        int bulkCount = products.Skip(beforeCount).Count(p => p.IsBulk);
                        Console.WriteLine($" Extracted {addedCount} products ({bulkCount} with bulk pricing) (Total: {products.Count})");
                    }
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" ERROR for {url}: {ex.Message}");
                    
                    // Save error screenshot if configured
                    if (_config.SaveErrorScreenshots)
                    {
                        Directory.CreateDirectory("output");
                        var path = Path.Combine("output", $"atb_error_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");
                        try { await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true }); } catch { }
                    }
                }

                // Add delay between pages if not the last URL
                if (i < catalogUrls.Count - 1)
                {
                    var waitTime = new Random().Next(15000, 25000);
                    if (_config.EnableLogging) Console.WriteLine($"Inter-page delay: {waitTime}ms ({waitTime/1000}s)");
                    await Task.Delay(waitTime);
                }
            }

            await context.CloseAsync();
            
            // Display final summary statistics
            int totalBulkProducts = products.Count(p => p.IsBulk);
            
            Console.WriteLine($"\n{new string('=', 60)}");
            Console.WriteLine($"SCRAPING COMPLETE");
            Console.WriteLine($"Total products extracted: {products.Count}");
            Console.WriteLine($"Products with bulk pricing: {totalBulkProducts}");
            Console.WriteLine($"{new string('=', 60)}");
            
            return products;
        }
    }
}