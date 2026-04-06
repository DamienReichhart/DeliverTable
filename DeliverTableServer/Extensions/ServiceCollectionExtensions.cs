using DeliverTableInfrastructure.Invoicing;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories;
using DeliverTableInfrastructure.Repositories.Interfaces;
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
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IRatingRepository, RatingRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IModerationRepository, ModerationRepository>();
        services.AddScoped<IOrderConfigRepository, OrderConfigRepository>();
        services.AddScoped<IEmailJobRepository, EmailJobRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IDisputeRepository, DisputeRepository>();
        services.AddScoped<IReclamationRepository, ReclamationRepository>();
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
        services.AddScoped<IAdminDiscountCodeService, AdminDiscountCodeService>();
        services.AddScoped<IAdminLoyaltyService, AdminLoyaltyService>();
        services.AddScoped<IAdminTransactionService, AdminTransactionService>();
        services.AddScoped<IAdminRatingService, AdminRatingService>();
        services.AddScoped<IAdminNotificationService, AdminNotificationService>();
        services.AddScoped<IAdminEventService, AdminEventService>();
        services.AddScoped<IAdminModerationService, AdminModerationService>();
        services.AddScoped<IAdminOrderConfigService, AdminOrderConfigService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IRestaurantService, RestaurantService>();
        services.AddScoped<IDishService, DishService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IRestaurantAccountService, RestaurantAccountService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<IDiscountCodeService, DiscountCodeService>();
        services.AddScoped<ILoyaltyService, LoyaltyService>();
        services.AddScoped<IEmailJobService, EmailJobService>();
        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IDisputeService, DisputeService>();
        services.AddScoped<IReclamationService, ReclamationService>();
        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<IReclamationService, ReclamationService>();
    }

    private static void RegisterInfrastructure(IServiceCollection services)
    {
        services.AddHttpClient<IGeoLocationService, GeoLocationService>();
        services.AddScoped<IStripeGateway, StripeGateway>();
        services.AddScoped<IInvoiceNumberingService, InvoiceNumberingService>();
    }
}
