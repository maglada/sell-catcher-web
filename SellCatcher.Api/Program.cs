using DotNetEnv;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using ProductScraper;
using SellCatcher.Api.DTOs;
using SellCatcher.Api.Models;
using SellCatcher.Api.Services;
using static SellCatcher.Api.Services.ParserProductService;

DotNetEnv.Env.Load(".env");

var builder = WebApplication.CreateBuilder(args);

var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
var jwtLifetime = Environment.GetEnvironmentVariable("JWT_TOKEN_LIFETIME");
var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "/app/data/sellcatcher.db";

// Ensure database directory exists
var dbDirectory = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
{
    Directory.CreateDirectory(dbDirectory);
}

// Log the database path being used
Console.WriteLine($"=== Database Configuration ===");
Console.WriteLine($"DB_PATH: {dbPath}");
Console.WriteLine($"Directory exists: {Directory.Exists(dbDirectory)}");

builder.Services.AddOpenApi();

// FIXED: Register ProductService as singleton with shared DB path
builder.Services.AddSingleton<ProductService>(sp => 
{
    var logger = sp.GetRequiredService<ILogger<ProductService>>();
    logger.LogInformation("Initializing ProductService with DB path: {DbPath}", dbPath);
    return new ProductService();
});

// Register all services with correct DB path
builder.Services.AddSingleton<AccountRepository>(sp => new AccountRepository(dbPath));
builder.Services.AddSingleton<ITokenRepository>(sp => new LiteDbTokenRepository(dbPath));

// Register DiscountService as singleton to share DB instance
builder.Services.AddSingleton<DiscountService>();

builder.Services.AddSingleton<ScraperFactory>();
builder.Services.AddControllers();

builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AuthSettings"));
builder.Services.AddScoped<JWTService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddScoped<StoreService>();

// CORS for ngrok
builder.Services.AddCors(o => o.AddPolicy("NgrokPolicy", p =>
{
    p.SetIsOriginAllowed(origin => 
        origin.Contains("ngrok-free.app") || 
        origin.Contains("ngrok.io") ||
        origin.Contains("localhost"))
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials();
}));

builder.Services.AddScoped<AccountService>();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Add health checks
builder.Services.AddHealthChecks();

// Configure forwarded headers for ngrok
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Verify database accessibility on startup
using (var scope = app.Services.CreateScope())
{
    var productService = scope.ServiceProvider.GetRequiredService<ProductService>();
    var stores = productService.GetStores();
    Console.WriteLine($"=== Database Check ===");
    Console.WriteLine($"Stores found: {stores.Count}");
    foreach (var store in stores)
    {
        var productCount = store.Categories.Sum(c => c.Products.Count);
        Console.WriteLine($"  - {store.Name}: {productCount} products");
    }
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors("NgrokPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseRouting();

// Map health checks
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();