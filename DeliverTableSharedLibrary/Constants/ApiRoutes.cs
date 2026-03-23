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

        /// <summary>Full paths for client HTTP calls.</summary>
        public const string UserMe = Base + "/" + UserMeRoute;
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
    }

    /// <summary>Test controller route (development only).</summary>
    public const string Test = "api/v1/test";
}
