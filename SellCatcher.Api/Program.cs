using DotNetEnv;
using Microsoft.OpenApi.Models;
using ProductScraper;
using SellCatcher.Api.DTOs;
using SellCatcher.Api.Models;
using SellCatcher.Api.Services;


DotNetEnv.Env.Load(".env");


var builder = WebApplication.CreateBuilder(args);


var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
var jwtLifetime = Environment.GetEnvironmentVariable("JWT_TOKEN_LIFETIME");
var dbPath = Environment.GetEnvironmentVariable("DB_PATH");



builder.Services.AddOpenApi();

builder.Services.AddSingleton<AccountRepository>(sp => new AccountRepository("sellcatcher.db"));
    
builder.Services.AddScoped<DiscountService>();
builder.Services.AddScoped<AccountRepository>();
builder.Services.AddControllers();
builder.Services.AddSingleton<ITokenRepository, LiteDbTokenRepository>();
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AuthSettings"));
builder.Services.AddScoped<JWTService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddAuth();
builder.Services.AddScoped<AccountService>();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var app = builder.Build();



app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRouting();
app.MapControllers();

app.Run();

// // namespace ProductScraperApp
// // {
// //     internal class Program
// //     {
// //         static async Task Main(string[] args)
// //         {
// //             var directory = args.Length > 0 ? args[0] : Path.Combine(Directory.GetCurrentDirectory(), "links");

// //             var config = new ScraperConfig
// //             {
// //                 Headless = true,
// //                 EnableLogging = true,
// //                 EnableDebugOutput = false,
// //                 SaveDebugScreenshots = false,
// //                 SaveErrorScreenshots = false,
// //                 SlowMo = 500
// //             };

// //             var factory = new ScraperFactory(config);

// //             try
// //             {
// //                 var results = await factory.ProcessAllFilesAsync(directory, "NovusLinks_*.txt");

// //                 foreach (var kvp in results)
// //                 {
// //                     Console.WriteLine($"File: {kvp.Key} -> Products: {kvp.Value.Count}");
// //                 }
// //             }
// //             catch (Exception ex)
// //             {
// //                 Console.WriteLine($"Fatal error: {ex}");
// //             }
// //         }
// //     }
// // }
// /// <summary>
// /// --------------Run Parser----------------
// /// </summary>
// // using static SellCatcher.Api.Services.ParserProductService;

// // class Test
// // {
// //    static async Task Main()
// //    {
// //        await TestParserRun();
// //    }

// //    static async Task TestParserRun()
// //    {
// //        Console.WriteLine("Start parsing Novus...\n");

// //        try
// //        {
// //            var factory = new ScraperFactory(new ScraperConfig
// //            {
// //                Headless = true,
// //                EnableLogging = true,
// //                EnableDebugOutput = false,
// //                SaveDebugScreenshots = false,
// //                SaveErrorScreenshots = false,
// //                SlowMo = 1000
// //            });

// //            string sitesFolder = Path.Combine(AppContext.BaseDirectory, "sites");

// //            var results = await factory.ProcessAllFilesAsync(
// //                directory: sitesFolder,
// //                filePattern: "NovusLinks_*.txt"
// //            );

// //            var productService = new ParserProductService.ProductService();
// //            int totalSaved = 0;
// //            int totalProducts = 0;

// //            foreach (var result in results)
// //            {
// //                string fileName = result.Key;
// //                var products = result.Value;
// //                totalProducts += products.Count;
// //                int saved = productService.SaveProducts(products, storeName: "NOVUS");
// //                totalSaved += saved;
// //            }

// //            Console.WriteLine($"\nTotal products parsed: {totalProducts}");
// //            Console.WriteLine($"Saved to DB: {totalSaved}\n");
// //            Console.WriteLine("Goods categories for NOVUS:\n");

// //            var categories = productService.GetCategories("NOVUS");

// //            foreach (var category in categories)
// //            {
// //                var categoryProducts = category.Products;
// //                var onSaleCount = categoryProducts.Count(p => p.IsOnSale);
// //                Console.WriteLine($"{category.Name}: {categoryProducts.Count} products ({onSaleCount} with discount)");
// //            }

// //            Console.WriteLine("\nParsing finished.");
// //        }
// //        catch (Exception ex)
// //        {
// //            Console.WriteLine(ex.Message);
// //        }

// //        Console.ReadKey();
// //    }
// // }