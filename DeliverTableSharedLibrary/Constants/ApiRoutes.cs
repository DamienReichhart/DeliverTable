namespace DeliverTableSharedLibrary.Constants;

/// <summary>
///     API route paths. Single source of truth for client and server; keep in sync with controller routes.
///     Uses nested static classes with <c>const</c> fields so values are usable in attributes and
///     validated at compile time (no dictionary key typos).
/// </summary>
public static class ApiRoutes
{
    /// <summary>Health endpoint path (relative to API base). Must match HealthController route.</summary>
    public const string Health = "api/v1/health";

    /// <summary>Storage proxy route patterns that serve objects directly from S3-compatible storage.</summary>
    public const string StorageImages = "images/{**path}";

    public const string StorageDocuments = "documents/{**path}";

    public const string LiveOrdersHub = "api/v1/live/orders";

    /// <summary>Authentication routes.</summary>
    public static class Auth
    {
        /// <summary>Controller base route. Use in <c>[Route(...)]</c>.</summary>
        public const string Base = "api/v1/auth";

        public const string LoginRoute = "login";
        public const string RegisterRoute = "register";
        public const string RestaurantRegisterRoute = "restaurant/register";
        public const string MeRoute = "me";
        public const string UpdateProfileRoute = "me";
        public const string ChangePasswordRoute = "me/password";

        /// <summary>Full paths for client HTTP calls.</summary>
        public const string Login = Base + "/" + LoginRoute;

        public const string Register = Base + "/" + RegisterRoute;
        public const string RestaurantRegister = Base + "/" + RestaurantRegisterRoute;
        public const string Me = Base + "/" + MeRoute;
        public const string UpdateProfile = Base + "/" + UpdateProfileRoute;
        public const string ChangePassword = Base + "/" + ChangePasswordRoute;
    }

    /// <summary>Restaurant routes.</summary>
    public static class Restaurant
    {
        /// <summary>Controller base route. Use in <c>[Route(...)]</c>.</summary>
        public const string Base = "api/v1/restaurant";

        public const string ByIdRoute = "{id:int}";
        public const string UserByIdRoute = "user/{id:int}";
        public const string UserMeRoute = "user/me";
        public const string MapRoute = "map";

        /// <summary>Full paths for client HTTP calls.</summary>
        public const string UserMe = Base + "/" + UserMeRoute;
        public const string Map = Base + "/" + MapRoute;
    }

    /// <summary>
    ///     Dish routes.
    /// </summary>
    public static class Dish
    {
        /// <summary>
        ///     Controller base route. Use in <c>[Route(...)]</c>.
        /// </summary>
        public const string Base = "api/v1/dish";

        public const string ByIdRoute = "{id:int}";
        public const string DishesByRestaurantIdRoute = "restaurant/{id:int}";

        /// <summary>
        ///     Full paths for client HTTP calls.
        /// </summary>
        public const string DishesByRestaurantId = Base + "/" + DishesByRestaurantIdRoute;

        public const string ImageRoute = "images/dish/";
    }

    /// <summary>Admin routes (Administrator role required).</summary>
    public static class Admin
    {
        /// <summary>Controller base route. Use in <c>[Route(...)]</c>.</summary>
        public const string Base = "api/v1/admin";

        public const string UsersRoute = "users";
        public const string UserByIdRoute = "users/{id:int}";
        public const string UserByIdRoleRoute = "users/{id:int}/role";
        public const string UserByIdStatusRoute = "users/{id:int}/status";

        /// <summary>Full paths for client HTTP calls.</summary>
        public const string Users = Base + "/" + UsersRoute;

        // ── Restaurants ──
        public const string RestaurantsRoute = "restaurants";
        public const string RestaurantByIdRoute = "restaurants/{id:int}";
        public const string RestaurantTablesRoute = "restaurants/{id:int}/tables";
        public const string RestaurantTableByIdRoute = "restaurants/{restaurantId:int}/tables/{tableId:int}";
        public const string Restaurants = Base + "/" + RestaurantsRoute;

        // ── Dishes ──
        public const string DishesRoute = "dishes";
        public const string DishByIdRoute = "dishes/{id:int}";
        public const string Dishes = Base + "/" + DishesRoute;

        // ── Orders ──
        public const string OrdersRoute = "orders";
        public const string OrderByIdRoute = "orders/{id:int}";
        public const string OrderStatusRoute = "orders/{id:int}/status";
        public const string OrderRefundRoute = "orders/{id:int}/refund";
        public const string Orders = Base + "/" + OrdersRoute;

        // ── Promotions ──
        public const string PromotionsRoute = "promotions";
        public const string PromotionByIdRoute = "promotions/{id:int}";
        public const string Promotions = Base + "/" + PromotionsRoute;

        // ── Discount Codes ──
        public const string DiscountCodesRoute = "discount-codes";
        public const string DiscountCodeByIdRoute = "discount-codes/{id:int}";
        public const string DiscountCodeRedemptionsRoute = "discount-codes/{id:int}/redemptions";
        public const string DiscountCodes = Base + "/" + DiscountCodesRoute;

        // ── Loyalty ──
        public const string LoyaltyRoute = "loyalty";
        public const string LoyaltyByIdRoute = "loyalty/{id:int}";
        public const string LoyaltyAccountsRoute = "loyalty/{id:int}/accounts";
        public const string LoyaltyTransactionsRoute = "loyalty/{id:int}/accounts/{accountId:int}/transactions";
        public const string LoyaltyPrograms = Base + "/" + LoyaltyRoute;

        // ── Events ──
        public const string EventsRoute = "events";
        public const string EventByIdRoute = "events/{id:int}";
        public const string Events = Base + "/" + EventsRoute;

        // ── Transactions ──
        public const string TransactionsRoute = "transactions";
        public const string TransactionByIdRoute = "transactions/{id:int}";
        public const string Transactions = Base + "/" + TransactionsRoute;

        // ── Ratings ──
        public const string RatingsRoute = "ratings";
        public const string RestaurantRatingsRoute = "ratings/restaurants";
        public const string RatingByIdRoute = "ratings/{id:int}";
        public const string Ratings = Base + "/" + RatingsRoute;

        // ── Notifications ──
        public const string NotificationsRoute = "notifications";
        public const string NotificationByIdRoute = "notifications/{id:int}";
        public const string Notifications = Base + "/" + NotificationsRoute;

        // ── Moderation ──
        public const string ModerationRoute = "moderation";
        public const string ModerationByIdRoute = "moderation/{id:int}";
        public const string Moderation = Base + "/" + ModerationRoute;

        // ── Order Config ──
        public const string OrderRulesRoute = "order-config/rules";
        public const string OrderRuleByIdRoute = "order-config/rules/{id:int}";
        public const string BlockedSlotsRoute = "order-config/blocked-slots";
        public const string BlockedSlotByIdRoute = "order-config/blocked-slots/{id:int}";
        public const string OrderRules = Base + "/" + OrderRulesRoute;
        public const string BlockedSlots = Base + "/" + BlockedSlotsRoute;

        // ── Dashboard ──
        public const string DashboardRoute = "dashboard";
        public const string Dashboard = Base + "/" + DashboardRoute;

        public const string DashboardAnalyticsRoute = "dashboard/analytics";
        public const string DashboardAnalytics = Base + "/" + DashboardAnalyticsRoute;

        // ── Invoices ──
        public const string InvoicesRoute = "invoices";
        public const string InvoiceByIdRoute = "invoices/{id:int}";

        // ── Disputes ──
        public const string DisputesRoute = "disputes";
        public const string DisputeByIdRoute = "disputes/{id:int}";
        public const string Disputes = Base + "/" + DisputesRoute;
    }

    /// <summary>Cart routes (Customer role required).</summary>
    public static class Cart
    {
        /// <summary>Controller base route. Use in <c>[Route(...)]</c>.</summary>
        public const string Base = "api/v1/cart";

        public const string ByRestaurantRoute = "restaurant/{id:int}";
        public const string ItemsRoute = "items";
        public const string ItemByIdRoute = "items/{id:int}";

        /// <summary>Full paths for client HTTP calls.</summary>
        public const string Items = Base + "/" + ItemsRoute;
    }

    /// <summary>Order routes (Authorized).</summary>
    public static class Order
    {
        /// <summary>Controller base route. Use in <c>[Route(...)]</c>.</summary>
        public const string Base = "api/v1/order";

        public const string ByIdRoute = "{id:int}";
        public const string StatusRoute = "{id:int}/status";
        public const string RestaurantOrdersRoute = "restaurant/{id:int}";
        public const string RatingRoute = "{orderId:int}/rating";
    }

    /// <summary>Restaurant account and transaction routes.</summary>
    public static class RestaurantAccount
    {
        public const string BaseRoute = "api/v1/restaurant/{id:int}/account";
        public const string WithdrawRoute = "withdraw";
    }

    /// <summary>Promotion routes (RestaurantOwner).</summary>
    public static class Promotion
    {
        public const string RestaurantBaseRoute = "api/v1/restaurant/{id:int}/promotions";
        public const string ActiveRoute = "api/v1/restaurant/{id:int}/promotions/active";
        public const string Base = "api/v1/promotion";
        public const string ByIdRoute = "{id:int}";
        public const string ById = Base + "/" + ByIdRoute;
    }

    /// <summary>Discount code routes (RestaurantOwner).</summary>
    public static class DiscountCode
    {
        public const string RestaurantBaseRoute = "api/v1/restaurant/{id:int}/discount-codes";
        public const string ValidateRoute = "api/v1/restaurant/{id:int}/discount-codes/validate";
        public const string Base = "api/v1/discount-code";
        public const string ByIdRoute = "{id:int}";
        public const string ById = Base + "/" + ByIdRoute;
    }

    /// <summary>Loyalty program routes.</summary>
    public static class Loyalty
    {
        public const string RestaurantBaseRoute = "api/v1/restaurant/{id:int}/loyalty";
        public const string MyAccountRoute = "my-account";
    }

    /// <summary>Invoice routes.</summary>
    public static class Invoice
    {
        public const string Base = "api/v1/invoice";
        public const string MyListRoute = "me";
        public const string RestaurantListRoute = "restaurant/{id:int}";
        public const string DownloadRoute = "{id:int}/pdf";
    }

    /// <summary>Dispute routes.</summary>
    public static class Dispute
    {
        public const string Base = "api/v1/dispute";
        public const string RestaurantListRoute = "restaurant/{id:int}";
    }

    /// <summary>Payment routes (Customer role required).</summary>
    public static class Payment
    {
        public const string Base = "api/v1/payment";
        public const string CancelRoute = "{orderId:int}/cancel";
    }

    /// <summary>Stripe webhook routes.</summary>
    public static class StripeWebhook
    {
        public const string Base = "api/v1/stripe";
        public const string WebhookRoute = "webhook";
        public const string Webhook = Base + "/" + WebhookRoute;
    }

    /// <summary>Test controller route (development only).</summary>
    public const string Test = "api/v1/test";
}
