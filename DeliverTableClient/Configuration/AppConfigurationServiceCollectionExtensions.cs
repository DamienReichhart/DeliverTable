using DeliverTableClient.Configuration.Interfaces;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DeliverTableClient.Configuration;

/// <summary>
/// Registers the centralized client configuration (IAppConfiguration) and the HttpClient used to load appconfig.json.
/// Call before AddApiClients so that API base URL comes from appconfig.
/// </summary>
public static class AppConfigurationServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IAppConfiguration"/> and the same-origin HttpClient used to load wwwroot/appconfig.json.
    /// </summary>
    public static IServiceCollection AddAppConfiguration(
        this IServiceCollection services,
        IWebAssemblyHostEnvironment hostEnvironment)
    {
        if (hostEnvironment == null)
            throw new ArgumentNullException(nameof(hostEnvironment));

        services.AddSingleton<IAppConfiguration>(sp =>
        {
            var env = sp.GetRequiredService<IWebAssemblyHostEnvironment>();
            var httpClient = new HttpClient { BaseAddress = new Uri(env.BaseAddress) };
            return new AppConfigurationImplementation(httpClient, env, env.BaseAddress);
        });

        return services;
    }
}
