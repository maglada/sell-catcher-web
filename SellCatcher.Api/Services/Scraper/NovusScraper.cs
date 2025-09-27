using System.IO;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

class Scraper
{
    public static async Task RunScraper(string[] args)
    {
        // Path to file with catalog URLs
        
        string filePath = "sites/NovusLinks.txt";
        var catalogUrls = File.ReadAllLines(filePath);

        // Create Playwright instance
        using var playwright = await Playwright.CreateAsync();
        
        // Launch Chromium in headless mode (with 1s slowdown to debug interactions)
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            SlowMo = 1000
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
        page.Console += (_, msg) => Console.WriteLine($"BROWSER: {msg.Text}");
        page.PageError += (_, err) => Console.WriteLine($"PAGE ERROR: {err}");

        foreach (var catalogUrl in catalogUrls)
        {
            if (string.IsNullOrWhiteSpace(catalogUrl)) continue;

            try
            {
                Console.WriteLine($"Navigating to catalog: {catalogUrl}");
                await page.GotoAsync(catalogUrl);
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                // Fetch the full HTML for debugging/inspection
                var html = await page.ContentAsync();
                Console.WriteLine($"Page HTML length: {html.Length}");

                // Save initial screenshot for debugging
                await page.ScreenshotAsync(new() { Path = "debug.png", FullPage = true });

                // Try to find product elements by multiple selector variations
                var productElements = await page.QuerySelectorAllAsync(
                    ".product-tile[data-testid*='product'], " +
                    ".product-card:not(.product-card__price), " +
                    "[class*='ProductTile'], " +
                    "[class*='jsx-a1615c42095f26c8 Price__value_caption Price__value_discount']"
                );
                
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

                    // DEBUG: Log any text that contains percentage to see what we're missing
                    if (text.Contains("%"))
                    {
                        Console.WriteLine($"DEBUG: Found text with percentage: '{text}'");
                    }

                    // DEBUG: Log any text that has multiple ₴ symbols (likely sale items)
                    if (Regex.Matches(text, @"₴").Count > 1)
                    {
                        Console.WriteLine($"DEBUG: Found text with multiple prices: '{text}'");
                    }

                    // Regex patterns:
                    // salePattern: Based on the actual format from debug output
                    var salePattern = @"([+\-]?\d+\s*%)\s*([\d.,]+)\s*₴\s*([\d.,]+)\s*₴до\s*(\d{2}\.\d{2})\s*(.+)";
                    
                    // normalPattern: just price + product name (excluding cases with multiple prices/discounts)
                    var normalPattern = @"^([\d.,]+)\s*₴\s*(?![\d.,]+\s*₴|до\s*\d|\d+\.\d+|.*%)(.*?)(?:\s+до\s+\d+\.\d+|\s+\d+\.\d+\s*₴|$)";

                    var saleMatch = Regex.Match(text, salePattern);
                    if (saleMatch.Success)
                    {
                        Console.WriteLine($"DEBUG: SALE PATTERN MATCHED! Groups: {saleMatch.Groups.Count}");
                        for (int i = 0; i < saleMatch.Groups.Count; i++)
                        {
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
                            var productKey = CreateProductKey(name, newPrice);
                            if (processedProducts.Add(productKey))
                            {
                                Console.WriteLine("=== SALE ITEM ===");
                                Console.WriteLine($"Знижка: {discount}");
                                Console.WriteLine($"Стара ціна: {oldPrice}");
                                Console.WriteLine($"Нова ціна: {newPrice}");
                                Console.WriteLine($"Діє до: {untilDate}");
                                Console.WriteLine($"Назва: {name}");
                                Console.WriteLine("==================");
                            }
                        }
                    }
                    else
                    {
                        // Also, let's make the sale pattern more flexible:
                        // Try these alternative patterns if the main one fails:
                        if (text.Contains("%") || Regex.Matches(text, @"₴").Count > 1)
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
                        
                        var normalMatch = Regex.Match(text, normalPattern);
                        if (normalMatch.Success)
                        {
                            // --- NORMAL ITEM ---
                            var price = normalMatch.Groups[1].Value.Trim();
                            var name = CleanProductName(normalMatch.Groups[2].Value);
                            
                            if (IsValidProduct(price, name))
                            {
                                var productKey = CreateProductKey(name, price);
                                if (processedProducts.Add(productKey))
                                {
                                    Console.WriteLine("=== NORMAL ITEM ===");
                                    Console.WriteLine($"Ціна: {price}");
                                    Console.WriteLine($"Назва: {name}");
                                    Console.WriteLine("==================");
                                }
                            }
                        }
                        // Log only if the text contains a price symbol but doesn't match patterns
                        else if (text.Contains("₴"))
                        {
                            Console.WriteLine($"DEBUG: No match for potential product: '{text}'");
                        }
                    }
                }

                // Save screenshot after processing
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = "final.png", FullPage = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error at {catalogUrl}: {ex.Message}");
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = "error.png", FullPage = true });
            }
        }

        await context.CloseAsync();
    }

    // --- HELPER METHODS ---

    /// <summary>
    /// Skip texts that are clearly not product data
    /// </summary>
    static bool ShouldSkipText(string text)
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
    static bool IsValidProduct(string price, string name)
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
    static bool IsValidPrice(string price)
    {
        if (string.IsNullOrWhiteSpace(price)) return false;
        
        var cleanPrice = price.Replace(" ", "").Replace(",", ".");
        if (!decimal.TryParse(cleanPrice, out decimal priceValue)) return false;
        
        return priceValue >= 0.01m && priceValue <= 100000m;
    }

    /// <summary>
    /// Cleans up product name: removes garbage, duplicate weights/volumes, trims whitespace
    /// </summary>
    static string CleanProductName(string name)
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
    static string CreateProductKey(string name, string price)
    {
        var cleanName = Regex.Replace(name.ToLower(), @"[^\w\s]", "");
        cleanName = Regex.Replace(cleanName, @"\s+", " ").Trim();
        
        return $"{cleanName}_{price}";
    }
}

