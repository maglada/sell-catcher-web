using System;
using System.IO;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ProductScraper
{
    public class NovusProductScraperAlc : IProductScraper
    {
        private readonly ScraperConfig _config;
        private readonly string _category;

        public NovusProductScraperAlc(ScraperConfig config = null, string category = null)
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

            foreach (var catalogUrl in catalogUrls)
            {
                if (string.IsNullOrWhiteSpace(catalogUrl)) continue;

                try
                {
                    if (_config.EnableLogging)
                        Console.WriteLine($"Navigating to catalog: {catalogUrl}");

                    var pageProducts = await ScrapeCatalogPageAsync(page, catalogUrl);
                    products.AddRange(pageProducts);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error at {catalogUrl}: {ex.Message}");
                    if (_config.SaveErrorScreenshots)
                        await page.ScreenshotAsync(new PageScreenshotOptions { Path = $"error_{DateTime.Now:yyyyMMddHHmmss}.png", FullPage = true });
                }
            }

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
                ".product-tile[data-testid*='product'], " +
                ".product-card:not(.product-card__price), " +
                "[class*='ProductTile'], " +
                "[class*='jsx-a1615c42095f26c8 Price__value_caption Price__value_discount']"
            );
            
            if (_config.EnableLogging)
                Console.WriteLine($"Found {productElements.Count} products");
            
            var seen = new HashSet<string>();           // Raw product texts already processed
            var processedProducts = new HashSet<string>(); // Products identified by unique key (name+price)

            foreach (var product in productElements)
            {
                var text = (await product.InnerTextAsync())?.Trim();
                if (string.IsNullOrEmpty(text)) continue;

                // Skip duplicate texts
                if (!seen.Add(text)) continue;

                // Normalize spaces and replace non-breaking space
                text = text.Replace("\u00A0", " ");
                text = Regex.Replace(text, @"\s+", " ");
                
                // Quick filtering to avoid noise
                if (ShouldSkipText(text)) continue;

                // Skip very short texts that are unlikely to be valid products
                if (text.Length < 10) continue;

                // DEBUG: Log any text that contains percentage to see what we're missing
                if (_config.EnableDebugOutput)
                {
                    if (text.Contains("%"))
                        Console.WriteLine($"DEBUG: Found text with percentage: '{text}'");
                    
                    // DEBUG: Log any text that has multiple ₴ symbols (likely sale items)
                    if (Regex.Matches(text, @"₴").Count > 1)
                        Console.WriteLine($"DEBUG: Found text with multiple prices: '{text}'");
                }

                var extractedProduct = ExtractProduct(text);
                if (extractedProduct != null)
                {
                    var productKey = CreateProductKey(extractedProduct.Name, extractedProduct.Price.ToString());
                    if (processedProducts.Add(productKey))
                    {
                        products.Add(extractedProduct);
                        
                        if (_config.EnableLogging)
                            PrintProduct(extractedProduct);
                    }
                }
                // Log only if the text contains a price symbol but doesn't match patterns
                else if (_config.EnableDebugOutput && text.Contains("₴"))
                {
                    Console.WriteLine($"DEBUG: No match for potential product: '{text}'");
                }
            }

            // Save screenshot after processing
            if (_config.SaveDebugScreenshots)
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = $"final_{DateTime.Now:yyyyMMddHHmmss}.png", FullPage = true });

            return products;
        }

        /// <summary>
        /// Extracts product information from raw text using regex patterns
        /// </summary>
        private Product ExtractProduct(string text)
        {
            // Regex patterns:
            // salePattern: Based on the actual format from debug output
            var salePattern = @"([+\-]?\d+\s*%)\s*([\d.,]+)\s*₴\s*([\d.,]+)\s*₴до\s*(\d{2}\.\d{2})\s*(.+)";
            var saleMatch = Regex.Match(text, salePattern);
            
            if (saleMatch.Success)
            {
                if (_config.EnableDebugOutput)
                {
                    Console.WriteLine($"DEBUG: SALE PATTERN MATCHED! Groups: {saleMatch.Groups.Count}");
                    for (int i = 0; i < saleMatch.Groups.Count; i++)
                        Console.WriteLine($"  Group {i}: '{saleMatch.Groups[i].Value}'");
                }
                
                // --- SALE ITEM ---
                var discount = saleMatch.Groups[1].Value.Trim();
                var oldPrice = saleMatch.Groups[2].Value.Trim(); // Already without ₴
                var newPrice = saleMatch.Groups[3].Value.Trim(); // Already without ₴
                var untilDate = saleMatch.Groups[4].Value.Trim();
                var name = CleanProductName(saleMatch.Groups[5].Value);

                if (IsValidProduct(newPrice, name))
                {
                    return new Product
                    {
                        Name = name,
                        Price = ParsePrice(newPrice),
                        OldPrice = ParsePrice(oldPrice),
                        Discount = discount,
                        ValidUntil = untilDate,
                        IsOnSale = true
                    };
                }
            }
            else
            {
                // Also, let's make the sale pattern more flexible:
                // Try these alternative patterns if the main one fails:
                if (_config.EnableDebugOutput && (text.Contains("%") || Regex.Matches(text, @"₴").Count > 1))
                {
                    TryAlternativeSalePatterns(text);
                }

                // normalPattern: just price + product name (excluding cases with multiple prices/discounts)
                var normalPattern = @"^([\d.,]+)\s*₴\s*(?![\d.,]+\s*₴|до\s*\d|\d+\.\d+|.*%)(.*?)(?:\s+до\s+\d+\.\d+|\s+\d+\.\d+\s*₴|$)";
                var normalMatch = Regex.Match(text, normalPattern);
                
                if (normalMatch.Success)
                {
                    // --- NORMAL ITEM ---
                    var price = normalMatch.Groups[1].Value.Trim();
                    var name = CleanProductName(normalMatch.Groups[2].Value);
                    
                    if (IsValidProduct(price, name))
                    {
                        return new Product
                        {
                            Name = name,
                            Price = ParsePrice(price),
                            IsOnSale = false
                        };
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Tries alternative regex patterns for sale items (for debugging purposes)
        /// </summary>
        private void TryAlternativeSalePatterns(string text)
        {
            // More flexible patterns to try:
            var alternativePatterns = new[]
            {
                @"([+\-]?\d+\s*%)\s*([\d.,]+)\s*₴\s*([\d.,]+)\s*₴.*?(\d{2}\.\d{2}).*?(.+)",  // More flexible spacing
                @"([+\-]?\d+\s*%)\s*([\d.,]+)\s*₴\s*([\d.,]+)\s*₴\s*(.+)",                    // Without date requirement
                @"([\d.,]+)\s*₴\s*([\d.,]+)\s*₴\s*([+\-]?\d+\s*%)\s*(.+)",                   // Percentage at end
                @"(.+?)\s*([+\-]?\d+\s*%)\s*([\d.,]+)\s*₴\s*([\d.,]+)\s*₴",                   // Name first
            };
            
            foreach (var pattern in alternativePatterns)
            {
                var altMatch = Regex.Match(text, pattern);
                if (altMatch.Success)
                {
                    Console.WriteLine($"DEBUG: ALTERNATIVE SALE PATTERN MATCHED: {pattern}");
                    Console.WriteLine($"DEBUG: Text was: '{text}'");
                    break;
                }
            }
        }

        /// <summary>
        /// Prints product information to console
        /// </summary>
        private void PrintProduct(Product product)
        {
            if (product.IsOnSale)
            {
                Console.WriteLine("=== SALE ITEM ===");
                Console.WriteLine($"Знижка: {product.Discount}");
                Console.WriteLine($"Стара ціна: {product.OldPrice}");
                Console.WriteLine($"Нова ціна: {product.Price}");
                Console.WriteLine($"Діє до: {product.ValidUntil}");
                Console.WriteLine($"Назва: {product.Name}");
                Console.WriteLine("==================");
            }
            else
            {
                Console.WriteLine("=== NORMAL ITEM ===");
                Console.WriteLine($"Ціна: {product.Price}");
                Console.WriteLine($"Назва: {product.Name}");
                Console.WriteLine("==================");
            }
        }

        // --- HELPER METHODS ---

        /// <summary>
        /// Skip texts that are clearly not product data
        /// </summary>
        private static bool ShouldSkipText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            
            // Pure numbers only (not valid products)
            if (Regex.IsMatch(text, @"^\s*\d+\s*$")) return true;
            
            // Very short texts that don't include a price
            if (text.Length < 5 && !text.Contains("₴")) return true;
            
            // No price and no letters → likely junk
            if (!text.Contains("₴") && !text.Any(char.IsLetter)) return true;
            
            return false;
        }

        /// <summary>
        /// Validates product data: checks that price & name are reasonable
        /// </summary>
        private static bool IsValidProduct(string price, string name)
        {
            if (!IsValidPrice(price)) return false;
            
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (name.Length < 3) return false;
            if (!name.Any(char.IsLetter)) return false;

            // Reject names that are mostly numbers (e.g., "123 456 789")
            var letterCount = name.Count(char.IsLetter);
            var digitCount = name.Count(char.IsDigit);
            if (digitCount > letterCount && letterCount < 3) return false;
            
            return true;
        }

        /// <summary>
        /// Checks that the price is numeric and within reasonable bounds
        /// </summary>
        private static bool IsValidPrice(string price)
        {
            if (string.IsNullOrWhiteSpace(price)) return false;
            
            var cleanPrice = price.Replace(" ", "").Replace(",", ".");
            if (!decimal.TryParse(cleanPrice, out decimal priceValue)) return false;
            
            return priceValue >= 0.01m && priceValue <= 100000m;
        }

        /// <summary>
        /// Parses price string to decimal value
        /// </summary>
        private static decimal ParsePrice(string price)
        {
            var cleanPrice = price.Replace(" ", "").Replace(",", ".");
            decimal.TryParse(cleanPrice, out decimal priceValue);
            return priceValue;
        }

        /// <summary>
        /// Cleans up product name: removes garbage, duplicate weights/volumes, trims whitespace
        /// </summary>
        private static string CleanProductName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            
            name = name.Trim();
            
            // Remove trailing patterns like "до 12.12", "250.0 ₴", or stray decimals
            name = Regex.Replace(name, @"\s*до\s*\d+\.\d+.*$", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\s*\d+\.\d+\s*₴.*$", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\s*\d+\.\d+\s*$", "", RegexOptions.IgnoreCase);
            
            // Deduplicate repeating weight/volume units
            name = Regex.Replace(name, @"(\d+\s*г)\s+\1\b", "$1");
            name = Regex.Replace(name, @"(\d+\s*мл)\s+\1\b", "$1");
            name = Regex.Replace(name, @"(\d+)\s*г\s+\1\s*г\b", "$1г");
            name = Regex.Replace(name, @"(\d+)\s*мл\s+\1\s*мл\b", "$1мл");
            
            // Legacy duplicate handling
            name = Regex.Replace(name, @"(\d+\s*г)(?:\s*\d+\s*г)+", "$1");
            name = Regex.Replace(name, @"(\d+\s*г)(?:\s*\1)+", "$1", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"(\d+\s*мл)(?:\s*\1)+", "$1", RegexOptions.IgnoreCase);
            
            // Normalize whitespace
            name = Regex.Replace(name, @"\s+", " ");
            
            return name.Trim();
        }

        /// <summary>
        /// Creates a unique key from product name + price (case-insensitive, symbols removed)
        /// </summary>
        private static string CreateProductKey(string name, string price)
        {
            var cleanName = Regex.Replace(name.ToLower(), @"[^\w\s]", "");
            cleanName = Regex.Replace(cleanName, @"\s+", " ").Trim();
            
            return $"{cleanName}_{price}";
        }
    }
}