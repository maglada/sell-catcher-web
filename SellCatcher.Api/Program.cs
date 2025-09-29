using SellCatcher.Api.Models;
using SellCatcher.Api.Services;
using SellCatcher.Api.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<AccountRepository>();
builder.Services.AddControllers();
builder.Services.AddScoped<AuthSettings>();
builder.Services.AddScoped<JWTService>();
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AuthSettings"));
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddScoped<AccountService>();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseRouting();  
app.MapControllers();

app.Run();
