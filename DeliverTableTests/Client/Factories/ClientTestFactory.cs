using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Auth;
using Microsoft.IdentityModel.Tokens;

namespace DeliverTableTests.Client.Factories;

/// <summary>
///     Factory methods that produce valid client-side DTOs for testing.
///     Tests start from a known-good state and mutate only the field under test.
/// </summary>
public static class ClientTestFactory
{
    public static string ValidToken => GenerateToken();
    public const string ValidRole = nameof(UserRole.Customer);
    public const string ValidUserId = "42";
    public const string ValidUserName = "Jean";

    private static string GenerateToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-test-secret-key-min-16-chars"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "test",
            audience: "test",
            claims: new[] { new Claim(ClaimTypes.Name, ValidUserName) },
            expires: DateTime.UtcNow.AddHours(1), // 👈 always fresh
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

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
