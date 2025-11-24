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


builder.Services.AddSingleton<ScraperFactory>();
builder.Services.AddScoped<DiscountService>();
builder.Services.AddScoped<AccountRepository>();
builder.Services.AddControllers();
builder.Services.AddSingleton<ITokenRepository, LiteDbTokenRepository>();
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AuthSettings"));
builder.Services.AddScoped<JWTService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddAuth();
builder.Services.AddCors(o => o.AddPolicy("LocalDev", p =>
  p.WithOrigins("http://localhost:5182")
  .AllowAnyHeader()
  .AllowAnyMethod()
  .AllowCredentials()));
builder.Services.AddScoped<AccountService>();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var app = builder.Build();

var factory = app.Services.GetRequiredService<ScraperFactory>();

app.UseCors("LocalDev");
var filepatterns = new List<string> { "NovusLinks_*.txt", "SilpoLinks_*.txt" };

foreach (var pattern in filepatterns)
{
    var results = await factory.ProcessAllFilesAsync(
        directory: Path.Combine(AppContext.BaseDirectory, "sites"),
        filePattern: pattern
    );

    var productService = new ParserProductService.ProductService();
    int totalSaved = 0;
    int totalProducts = 0;

    foreach (var result in results)
    {
        string storeName = pattern.Contains("Novus") ? "NOVUS" : "SILPO";
        var products = result.Value;
        totalProducts += products.Count;
        int saved = productService.SaveProducts(products, storeName);
        totalSaved += saved;
    }

    Console.WriteLine($"\nTotal products parsed: {totalProducts} from pattern {pattern}");
    Console.WriteLine($"Saved to DB: {totalSaved}\n");
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRouting();
app.MapControllers();

app.Run();


/// <summary>
/// --------------Run Parser----------------
/// </summary>

// class Test
// {
//     static async Task Main()
//     {
//         Console.WriteLine("Enter store name to parse (Novus / Silpo):");
//         string storeName = Console.ReadLine()?.Trim().ToUpper() ?? "NOVUS";

//         await TestParserRun(storeName);
//     }

//     static async Task TestParserRun(string storeName)
//     {
//         Console.WriteLine($"Starting parsing {storeName}...\n");

//         try
//         {
//             var config = new ScraperConfig
//             {
//                 Headless = true,
//                 EnableLogging = true,
//                 EnableDebugOutput = false,
//                 SaveDebugScreenshots = false,
//                 SaveErrorScreenshots = false,
//                 SlowMo = 1000
//             };

//             var factory = new ScraperFactory(config);
//             string sitesFolder = Path.Combine(AppContext.BaseDirectory, "sites");

//             string filePattern = storeName switch
//             {
//                 "NOVUS" => "NovusLinks_*.txt",
//                 "SILPO" => "SilpoLinks_*.txt",
//                 _ => throw new ArgumentException("Unknown store!")
//             };

//             var results = await factory.ProcessAllFilesAsync(
//                 directory: sitesFolder,
//                 filePattern: filePattern
//             );

//             var productService = new ParserProductService.ProductService();
//             int totalSaved = 0;
//             int totalProducts = 0;

//             foreach (var result in results)
//             {
//                 var products = result.Value;
//                 totalProducts += products.Count;
//                 int saved = productService.SaveProducts(products, storeName);
//                 totalSaved += saved;
//             }

//             Console.WriteLine($"\nTotal products parsed: {totalProducts} from {storeName}");
//             Console.WriteLine($"Saved to DB: {totalSaved}\n");

//             var categories = productService.GetCategories(storeName);
//             Console.WriteLine($"Categories for {storeName}:");

//             foreach (var category in categories)
//             {
//                 int count = category.Products.Count;
//                 int onSaleCount = category.Products.Count(p => p.IsOnSale);
//                 Console.WriteLine($"  {category.Name}: {count} products ({onSaleCount} with discount)");
//             }

//             Console.WriteLine("\nParsing finished.");
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine(ex.Message);
//         }

//         Console.ReadKey();
//     }
// }
