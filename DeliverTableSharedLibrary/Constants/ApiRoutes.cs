namespace DeliverTableSharedLibrary.Constants;

/// <summary>
///     API route paths. Single source of truth for client and server; keep in sync with controller routes.
/// </summary>
public static class ApiRoutes
{
    /// <summary>Health endpoint path (relative to API base). Must match HealthController route.</summary>
    public const string Health = "api/v1/health";
    
    // Auth
    public static readonly Dictionary<string, string> Auth = new()
    {
        { "Login", "api/auth/login" },
        { "Register", "api/auth/register" },
        { "RestaurantRegister", "api/auth/restaurant/register" }
    };
}