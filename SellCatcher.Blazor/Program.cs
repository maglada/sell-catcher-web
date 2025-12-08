using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SellCatcher.Blazor;
using SellCatcher.Blazor.Components;
//using Microsoft.OpenApi.Models;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiUrl = builder.Configuration["ApiUrl"] ?? "http://localhost:5000";

builder.Services.AddScoped(sp => 
{
    var client = new HttpClient { BaseAddress = new Uri(apiUrl) };
    // Add ngrok bypass header
    client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
    return client;
});

await builder.Build().RunAsync();

//var app = builder.Build();
//app.UseHttpsRedirection();