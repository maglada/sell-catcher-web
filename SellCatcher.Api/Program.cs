using SellCatcher.Api.Models;
using SellCatcher.Api.Services;
using DotNetEnv;
using SellCatcher.Api.DTOs;
using Microsoft.OpenApi.Models;
DotNetEnv.Env.Load(".env");


var builder = WebApplication.CreateBuilder(args);


var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
var jwtLifetime = Environment.GetEnvironmentVariable("JWT_TOKEN_LIFETIME");
var dbPath = Environment.GetEnvironmentVariable("DB_PATH");

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



app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRouting();  
app.MapControllers();

app.Run();



