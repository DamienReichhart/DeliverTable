using DeliverTableClient.Configuration;
using DeliverTableClient.Configuration.Interfaces;
using DeliverTableClient.Services;
using DeliverTableClient.Services.Interfaces;

namespace DeliverTableClient.Extensions;

/// <summary>
///     Extension methods for registering API options and HTTP clients.
///     API base URL is read from <see cref="IAppConfiguration" /> (appconfig.json). Call
///     <see cref="Configuration.AppConfigurationServiceCollectionExtensions.AddAppConfiguration" /> first.
/// </summary>
public static class ApiClientServiceCollectionExtensions
{
    /// <summary>
    ///     Registers API client options from <see cref="IAppConfiguration" /> and the shared API <see cref="HttpClient" /> and
    ///     all API client implementations.
    ///     Requires <see cref="IAppConfiguration" /> to be registered and loaded (via
    ///     <see cref="Configuration.AppConfigurationServiceCollectionExtensions.AddAppConfiguration" /> and LoadAsync before
    ///     RunAsync).
    /// </summary>
    public static IServiceCollection AddApiClients(this IServiceCollection services)
    {
        services.AddSingleton<IApiClientOptions>(sp =>
        {
            var appConfig = sp.GetRequiredService<IAppConfiguration>();
            return new ApiClientOptions { BaseUrl = appConfig.ApiBaseUrl };
        });

        services.AddScoped(sp =>
        {
            var options = sp.GetRequiredService<IApiClientOptions>();
            var client = new HttpClient { BaseAddress = new Uri(options.BaseUrl) };
            return client;
        });

        RegisterApiClients(services);
        RegisterRestaurantService(services);
        RegisterUserService(services);
        RegisterDishService(services);
        RegisterAdminService(services);
        RegisterCartService(services);
        RegisterOrderService(services);
        RegisterRestaurantAccountService(services);
        RegisterPromotionService(services);
        RegisterDiscountCodeClientService(services);
        RegisterLoyaltyClientService(services);
        RegisterAdminDomainServices(services);

        return services;
    }

    /// <summary>
    ///     Registers API client implementations. Add new clients here as the app grows.
    /// </summary>
    private static void RegisterApiClients(IServiceCollection services)
    {
        services.AddScoped<IHealthApiClient, HealthApiClient>();
    }

    private static void RegisterRestaurantService(IServiceCollection services)
    {
        services.AddScoped<IRestaurantService, RestaurantService>();
    }

    private static void RegisterUserService(IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
    }

    private static void RegisterDishService(IServiceCollection services)
    {
        services.AddScoped<IDishService, DishService>();
    }

    private static void RegisterAdminService(IServiceCollection services)
    {
        services.AddScoped<IAdminService, AdminService>();
    }

    private static void RegisterCartService(IServiceCollection services)
    {
        services.AddScoped<ICartService, CartService>();
    }

    private static void RegisterOrderService(IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
    }

    private static void RegisterRestaurantAccountService(IServiceCollection services)
    {
        services.AddScoped<IRestaurantAccountService, RestaurantAccountService>();
    }

    private static void RegisterPromotionService(IServiceCollection services)
    {
        services.AddScoped<IPromotionService, PromotionService>();
    }

    private static void RegisterDiscountCodeClientService(IServiceCollection services)
    {
        services.AddScoped<IDiscountCodeClientService, DiscountCodeClientService>();
    }

    private static void RegisterLoyaltyClientService(IServiceCollection services)
    {
        services.AddScoped<ILoyaltyClientService, LoyaltyClientService>();
    }

    private static void RegisterAdminDomainServices(IServiceCollection services)
    {
        services.AddScoped<IAdminRestaurantClientService, AdminRestaurantClientService>();
        services.AddScoped<IAdminDishClientService, AdminDishClientService>();
        services.AddScoped<IAdminOrderClientService, AdminOrderClientService>();
        services.AddScoped<IAdminPromotionClientService, AdminPromotionClientService>();
        services.AddScoped<IAdminDiscountCodeClientService, AdminDiscountCodeClientService>();
        services.AddScoped<IAdminLoyaltyClientService, AdminLoyaltyClientService>();
        services.AddScoped<IAdminEventClientService, AdminEventClientService>();
        services.AddScoped<IAdminTransactionClientService, AdminTransactionClientService>();
        services.AddScoped<IAdminRatingClientService, AdminRatingClientService>();
        services.AddScoped<IAdminNotificationClientService, AdminNotificationClientService>();
        services.AddScoped<IAdminModerationClientService, AdminModerationClientService>();
        services.AddScoped<IAdminOrderConfigClientService, AdminOrderConfigClientService>();
        services.AddScoped<IAdminDashboardClientService, AdminDashboardClientService>();
    }
}