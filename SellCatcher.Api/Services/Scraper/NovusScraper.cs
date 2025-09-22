using Microsoft.Playwright;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.AccessControl;
using Microsoft.Extensions.Options;

class Scraper
{
    public static async Task Main(string[] args)
    {
        var catalogUrl = "https://novus.zakaz.ua/uk/";

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

        try
        {
            Console.WriteLine("Navigating to catalog...");
            await page.GotoAsync(catalogUrl);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // debug current page
            var html = await page.ContentAsync();
            Console.WriteLine($"Page HTML length: {html.Length}");
            await page.ScreenshotAsync(new() { Path = "debug.png", FullPage = true });

            // multiple selectors for products
            var selectors = new[]
            {
                ".ProductsBox",
                ".products-box",
                ".product-tile",
                "[class*='ProductTile']",
                "[class*='jsx-a1615c42095f26c8 Price__value_caption Price__value_discount']"
            };

            foreach (var selector in selectors)
            {
                Console.WriteLine($"Trying selector: {selector}");
                var elements = await page.QuerySelectorAllAsync(selector);
                if (elements.Count > 0)
                    {
                    Console.WriteLine($"Found {elements.Count} elements with selector {selector}");

                    foreach (var element in elements)
                    {
                        var text = (await element.InnerTextAsync())?.Trim();
                        if (string.IsNullOrEmpty(text))
                        {
                            continue;
                        }

                        text = text.Replace("\u00A0", " ");
                        text = Regex.Replace(text, @"\s+", " ");

                        // !!!!SPECIFICALLY THIS TEXT RIGHT HERE is what we want!!!! otherwise it spams usless info
                        var pattern = @"([+\-]?\d+\s*%)?\s*([\d.,]+)\s*₴\s*([\d.,]+)\s*₴\s*до\s*(\d{2}\.\d{2})\s*(.+)";
                        var match = Regex.Match(text, pattern);

                        // process only elements that match the regex
                        if (!match.Success)
                        {
                            continue;
                        }

                        Console.WriteLine($"Raw text: {text}");

                        var discount = match.Groups[1].Value.Trim();
                        var oldPrice = match.Groups[2].Value.Trim();
                        var newPrice = match.Groups[3].Value.Trim();
                        var untilDate = match.Groups[4].Value.Trim();
                        var name = match.Groups[5].Value.Trim();

                        name = Regex.Replace(name, @"(\d+\s*г)(?:\s*\1)+$", "$1");

                        Console.WriteLine($"Знижка: {discount}");
                        Console.WriteLine($"Стара ціна: {oldPrice}");
                        Console.WriteLine($"Нова ціна: {newPrice}");
                        Console.WriteLine($"Діє до: {untilDate}");
                        Console.WriteLine($"Назва: {name}");
                    }

                        break;
                    }
                }

                // save final state for debugging. can be removed later
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = "final.png", FullPage = true });
            }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = "error.png", FullPage = true });
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}