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
builder.Services.AddSingleton<ScraperFactory>();
builder.Services.AddControllers();
builder.Services.AddSingleton<ITokenRepository, LiteDbTokenRepository>();
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AuthSettings"));
builder.Services.AddScoped<JWTService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddCors(o => o.AddPolicy("LocalDev", p =>
  p.WithOrigins("http://localhost:5182")
  .AllowAnyHeader()
  .AllowAnyMethod()
  .AllowCredentials()));
builder.Services.AddScoped<AccountService>();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var app = builder.Build();

//var factory = app.Services.GetRequiredService<ScraperFactory>();

//app.UseCors("LocalDev");
//var filepatterns = new List<string> { "NovusLinks_*.txt", "SilpoLinks_*.txt" };

//foreach (var pattern in filepatterns)
//{
//    var results = await factory.ProcessAllFilesAsync(
//        directory: Path.Combine(AppContext.BaseDirectory, "sites"),
//        filePattern: pattern
//    );

//    var productService = new ParserProductService.ProductService();
//    int totalSaved = 0;
//    int totalProducts = 0;

//    foreach (var result in results)
//    {
//        string storeName = pattern.Contains("Novus") ? "Novus" : "Silpo";
//        var products = result.Value;
//        totalProducts += products.Count;
//        int saved = productService.SaveProducts(products, storeName);
//        totalSaved += saved;
//    }

//    Console.WriteLine($"\nTotal products parsed: {totalProducts} from pattern {pattern}");
//    Console.WriteLine($"Saved to DB: {totalSaved}\n");
//}


app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRouting();
app.MapControllers();

app.Run();

