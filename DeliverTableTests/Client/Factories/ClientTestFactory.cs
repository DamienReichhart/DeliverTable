using DeliverTableSharedLibrary.Dtos.Auth;

namespace DeliverTableTests.Client.Factories;

/// <summary>
///     Factory methods that produce valid client-side DTOs for testing.
///     Tests start from a known-good state and mutate only the field under test.
/// </summary>
public static class ClientTestFactory
{
    public const string ValidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test-payload.test-signature";
    public const string ValidRole = "Customer";
    public const string ValidUserId = "42";
    public const string ValidUserName = "Jean";

    /// <summary>Creates a valid <see cref="ConnectionResponse" /> with token and user.</summary>
    public static ConnectionResponse CreateValidConnectionResponse() => new()
    {
        Token = ValidToken,
        User = CreateValidUserResponse()
    };

    /// <summary>Creates a valid <see cref="UserResponse" /> for a customer.</summary>
    public static UserResponse CreateValidUserResponse() => new()
    {
        Id = 42,
        Email = "jean.dupont@example.com",
        FirstName = ValidUserName,
        LastName = "Dupont",
        Role = ValidRole
    };

    /// <summary>Creates a <see cref="ConnectionResponse" /> with an empty token to test the null/empty guard.</summary>
    public static ConnectionResponse CreateConnectionResponseWithEmptyToken() => new()
    {
        Token = "",
        User = CreateValidUserResponse()
    };

    /// <summary>Creates the JSON error body the API returns on failure (mirrors AuthService.ApiErrorResponse).</summary>
    public static object CreateApiErrorBody(string error) => new { Error = error };
}
