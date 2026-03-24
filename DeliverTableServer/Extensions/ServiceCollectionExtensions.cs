using DeliverTableServer.Repositories;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;

namespace DeliverTableServer.Extensions;

/// <summary>
///     Centralized registration of application services. Add new service registrations here to keep Program.cs minimal.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers all DeliverTable application services, repositories, and infrastructure.
    /// </summary>
    public static IServiceCollection AddDeliverTableServices(this IServiceCollection services)
    {
        RegisterRepositories(services);
        RegisterServices(services);
        RegisterInfrastructure(services);
        return services;
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRestaurantRepository, RestaurantRepository>();
        services.AddScoped<IDishRepository, DishRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IRestaurantTransactionRepository, RestaurantTransactionRepository>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();
        services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAdminRestaurantService, AdminRestaurantService>();
        services.AddScoped<IAdminDishService, AdminDishService>();
        services.AddScoped<IAdminPromotionService, AdminPromotionService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IRestaurantService, RestaurantService>();
        services.AddScoped<IDishService, DishService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IRestaurantAccountService, RestaurantAccountService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<IDiscountCodeService, DiscountCodeService>();
        services.AddScoped<ILoyaltyService, LoyaltyService>();
    }

    private static void RegisterInfrastructure(IServiceCollection services)
    {
        services.AddHttpClient<IGeoLocationService, GeoLocationService>();
    }
}
