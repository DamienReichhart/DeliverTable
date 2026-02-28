namespace DeliverTableSharedLibrary.Constants;

/// <summary>
///     API route paths. Single source of truth for client and server; keep in sync with controller routes.
/// </summary>
public static class ApiRoutes
{
    /// <summary>Health endpoint path (relative to API base). Must match HealthController route.</summary>
    public const string Health = "api/v1/health";

    public const string Authentication = "api/v1/auth";

    public const string Restaurant = "api/v1/restaurant";

    public static readonly Dictionary<string, string> RestaurantEndpoints = new()
    {
        { "All", "api/v1/restaurant" },
        { "Single", "api/v1/restaurant/" },
        { "Create", "api/v1/restaurant" },
        { "Update", "api/v1/restaurant/" },
        { "Delete", "api/v1/restaurant/" },
    };
    
    // Auth
    public static readonly Dictionary<string, string> Auth = new()
    {
        { "Login", "api/v1/auth/login" },
        { "Register", "api/v1/auth/register" },
        { "RestaurantRegister", "api/v1/auth/restaurant/register" }
    };
}