using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using DeliverTableClient;
using DeliverTableClient.Configuration;
using DeliverTableClient.Configuration.Interfaces;
using DeliverTableClient.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAppConfiguration(builder.HostEnvironment);
builder.Services.AddApiClients();

var host = builder.Build();

// Load centralized config from wwwroot/appconfig.json before the app runs.
await host.Services.GetRequiredService<IAppConfiguration>().LoadAsync();

await host.RunAsync();