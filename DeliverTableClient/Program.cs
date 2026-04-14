using DeliverTableClient;
using DeliverTableClient.Configuration;
using DeliverTableClient.Configuration.Interfaces;
using DeliverTableClient.Extensions;
using DeliverTableClient.Services.Auth;
using DeliverTableClient.Services.Payment;
using DeliverTableClient.State;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<ApiAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ApiAuthStateProvider>());
builder.Services.AddScoped<AuthService>();
builder.Services.AddAuthorizationCore();

builder.Services.AddAppConfiguration(builder.HostEnvironment);
builder.Services.AddApiClients();
builder.Services.AddScoped<IStripeJsInterop, StripeJsInterop>();
builder.Services.AddScoped<CheckoutState>();

var host = builder.Build();

// Load centralized config from wwwroot/appconfig.json before the app runs.
await host.Services.GetRequiredService<IAppConfiguration>().LoadAsync();

await host.RunAsync();