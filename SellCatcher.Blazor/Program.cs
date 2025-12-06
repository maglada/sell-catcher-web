using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SellCatcher.Blazor;
using SellCatcher.Blazor.Components;
//using Microsoft.OpenApi.Models;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(" https://monte-warier-minisculely.ngrok-free.dev") });

await builder.Build().RunAsync();

//var app = builder.Build();
//app.UseHttpsRedirection();