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

Console.WriteLine($"=== Configuration ===");
Console.WriteLine($"DB_PATH: {dbPath}");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

builder.Services.AddOpenApi();

// Register services
builder.Services.AddSingleton<ProductService>(sp => 
{
    var logger = sp.GetRequiredService<ILogger<ProductService>>();
    logger.LogInformation("Initializing ProductService with DB path: {DbPath}", dbPath);
    return new ProductService();
});

builder.Services.AddSingleton<AccountRepository>(sp => new AccountRepository(dbPath));
builder.Services.AddSingleton<ITokenRepository>(sp => new LiteDbTokenRepository(dbPath));
builder.Services.AddSingleton<DiscountService>();
builder.Services.AddSingleton<ScraperFactory>();
builder.Services.AddControllers();

builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AuthSettings"));
builder.Services.AddScoped<JWTService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddScoped<StoreService>();
builder.Services.AddScoped<AccountService>();

// CRITICAL: Enhanced CORS for Docker + local development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });

    // For prod (Docker / real hosting, including ngrok front)
    options.AddPolicy("Production", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",          // local front
                "http://localhost:5182",          // maybe VS dev server
                "http://frontend:3000",           // docker service
                "https://your-frontend-ngrok.ngrok-free.dev" // ⬅ add your real ngrok frontend URL here
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// Configure forwarded headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Database verification
using (var scope = app.Services.CreateScope())
{
    try
    {
        var productService = scope.ServiceProvider.GetRequiredService<ProductService>();
        var stores = productService.GetStores();
        Console.WriteLine($"=== Database Status ===");
        Console.WriteLine($"Stores: {stores.Count}");
        foreach (var store in stores)
        {
            var productCount = store.Categories.Sum(c => c.Products.Count);
            Console.WriteLine($"  - {store.Name}: {productCount} products");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Database verification failed: {ex.Message}");
    }
}

app.UseRouting();

app.UseForwardedHeaders();

// Only ONE CORS call
app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "Production");

// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

Console.WriteLine("🚀 API starting on http://+:5000");
app.Run();