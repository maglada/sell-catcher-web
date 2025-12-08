using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SellCatcher.Blazor;
using SellCatcher.Blazor.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// CRITICAL: Get API URL from config or environment
// This allows both local dev and Docker deployment
var apiUrl = builder.Configuration["ApiUrl"] ?? "http://localhost:5000";

Console.WriteLine($"🔧 Configuring API URL: {apiUrl}");

builder.Services.AddScoped(sp => 
{
    // BLAZOR WASM RUNS IN THE BROWSER!
    // We need to use the PUBLIC URL, not Docker internal URLs
    var actualUrl = apiUrl;
    
    // If running in browser and API URL is Docker internal, use localhost
    if (actualUrl.Contains("api:5000"))
    {
        actualUrl = "http://localhost:5000";
        Console.WriteLine($"⚠️ Detected Docker URL, switching to: {actualUrl}");
    }
    
    var client = new HttpClient { BaseAddress = new Uri(actualUrl) };
    
    // Add ngrok bypass header
    client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
    
    Console.WriteLine($"✅ HttpClient configured with base URL: {client.BaseAddress}");
    
    return client;
});

await builder.Build().RunAsync();
