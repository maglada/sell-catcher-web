using System.IO;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;

class Scraper
{
    public static async Task Main(string[] args)
    {
        string filePath = "NovusLinks.txt";
        var catalogUrls = File.ReadAllLines(filePath);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            SlowMo = 1000
        });

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

                var html = await page.ContentAsync();
                Console.WriteLine($"Page HTML length: {html.Length}");
                await page.ScreenshotAsync(new() { Path = "debug.png", FullPage = true });

                var productElements = await page.QuerySelectorAllAsync(
                    ".product-tile[data-testid*='product'], " +
                    ".product-card:not(.product-card__price), " +
                    "[class*='ProductTile'], " +
                    "[class*='jsx-a1615c42095f26c8 Price__value_caption Price__value_discount']"
                    );
                
                Console.WriteLine($"Found {productElements.Count} products");
                
                var seen = new HashSet<string>();
                var processedProducts = new HashSet<string>();

                foreach (var product in productElements)
                {
                    var text = (await product.InnerTextAsync())?.Trim();
                    if (string.IsNullOrEmpty(text)) continue;

                    if (!seen.Add(text)) continue;

                    text = text.Replace("\u00A0", " ");
                    text = Regex.Replace(text, @"\s+", " ");

                    var salePattern = @"([+\-]?\d+\s*%)\s*([\d.,]+)\s*₴\s*([\d.,]+)\s*₴\s*до\s*(\d{2}\.\d{2})\s*(.+)";
                    var normalPattern = @"^([\d.,]+)\s*₴\s*(?![\d.,]+\s*₴)(.+)$";

                    var saleMatch = Regex.Match(text, salePattern);
                    if (saleMatch.Success)
                    {
                        // --- SALE ITEM ---
                        var discount = saleMatch.Groups[1].Value.Trim();
                        var oldPrice = saleMatch.Groups[2].Value.Trim();
                        var newPrice = saleMatch.Groups[3].Value.Trim();
                        var untilDate = saleMatch.Groups[4].Value.Trim();
                        var name = saleMatch.Groups[5].Value.Trim();

                        name = Regex.Replace(name, @"(\d+\s*г)(?:\s*\d+\s*г)+", "$1");
                        name = Regex.Replace(name, @"(\d+\s*г)(?:\s*\1)+", "$1", RegexOptions.IgnoreCase);
                        name = Regex.Replace(name, @"(\d+\s*мл)(?:\s*\1)+", "$1", RegexOptions.IgnoreCase);

                        var productKey = $"{name}_{newPrice}";
                        if (processedProducts.Add(productKey)) // Only process if not seen before
                        {
                            Console.WriteLine("=== SALE ITEM ===");
                            Console.WriteLine($"Знижка: {discount}");
                            Console.WriteLine($"Стара ціна: {oldPrice}");
                            Console.WriteLine($"Нова ціна: {newPrice}");
                            Console.WriteLine($"Діє до: {untilDate}");
                            Console.WriteLine($"Назва: {name}");
                        }
                        continue;
                    }
                    else if (Regex.Match(text, normalPattern) is Match normalMatch && normalMatch.Success)
                    {
                        // --- NORMAL ITEM ---
                        var price = normalMatch.Groups[1].Value.Trim();
                        var name = normalMatch.Groups[2].Value.Trim();

                        name = Regex.Replace(name, @"(\d+\s*г)(?:\s*\1)+$", "$1");

                        var productKey = $"{name}_{price}";
                        if (processedProducts.Add(productKey)) // Only process if not seen before
                        {
                            Console.WriteLine("=== NORMAL ITEM ===");
                            Console.WriteLine($"Ціна: {price}");
                            Console.WriteLine($"Назва: {name}");
                        }
                    }

                    else
                    {
                        Console.WriteLine("No match for product text.");
                    }
                }

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
}
