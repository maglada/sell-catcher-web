using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using SellCatcher.Blazor;
using SellCatcher.Blazor.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API URL configuration
var apiUrl = builder.Configuration["ApiUrl"] ?? "http://localhost:5000";
Console.WriteLine($"🔧 Configuring API URL: {apiUrl}");

builder.Services.AddScoped(sp => 
{
    var actualUrl = apiUrl;
    
    if (actualUrl.Contains("api:5000"))
    {
        actualUrl = "http://localhost:5000";
        Console.WriteLine($"⚠️ Detected Docker URL, switching to: {actualUrl}");
    }
    
    var client = new HttpClient { BaseAddress = new Uri(actualUrl) };
    client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
    
    Console.WriteLine($"✅ HttpClient configured with base URL: {client.BaseAddress}");
    
    return client;
});

// Register authentication services
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(s => 
    s.GetRequiredService<CustomAuthStateProvider>());

await builder.Build().RunAsync();