using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;

namespace DeliverTableServer.Extensions;

/// <summary>
///     Centralized registration of application services. Add new service registrations here to keep Program.cs minimal.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers all DeliverTable application services (health, and future domain/application services).
    /// </summary>
    public static IServiceCollection AddDeliverTableServices(this IServiceCollection services)
    {
        RegisterHealthServices(services);
        RegisterTokenService(services);
        return services;
    }

    private static void RegisterHealthServices(IServiceCollection services)
    {
        services.AddScoped<IHealthService, HealthService>();
    }

    private static void RegisterTokenService(IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
    }
}