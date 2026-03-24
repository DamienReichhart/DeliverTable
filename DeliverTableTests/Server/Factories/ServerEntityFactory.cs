using DeliverTableServer.Configuration;
using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableTests.Server.Factories;

/// <summary>
///     Factory methods that produce valid server-side entities.
///     Tests start from a known-good state and mutate only the field under test.
/// </summary>
public static class ServerEntityFactory
{
    private static int _emailCounter;

    /// <summary>Creates a valid <see cref="User"/> with unique email to avoid DB constraint conflicts.</summary>
    public static User CreateValidUser(string? email = null)
    {
        var resolvedEmail = email ?? $"user{Interlocked.Increment(ref _emailCounter)}@example.com";

        return new User
        {
            UserName = resolvedEmail,
            Email = resolvedEmail,
            NormalizedEmail = resolvedEmail.ToUpperInvariant(),
            NormalizedUserName = resolvedEmail.ToUpperInvariant(),
            FirstName = "Test",
            LastName = "User",
            Status = UserStatus.Active,
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }

    /// <summary>Creates a valid <see cref="User"/> with an attached <see cref="Customer"/>.</summary>
    public static User CreateValidCustomer(string? email = null)
    {
        var user = CreateValidUser(email);
        user.Customer = new Customer();
        return user;
    }

    /// <summary>Creates a valid <see cref="User"/> with an attached <see cref="RestaurantOwner"/> profile.</summary>
    public static User CreateValidRestaurantOwner(string? email = null)
    {
        var user = CreateValidUser(email);
        user.RestaurantOwner = new RestaurantOwner
        {
            CompanyName = "Le Bon Restaurant",
            VatNumber = "BE0123456789",
            ContactPhoneNumber = "+32470123456"
        };
        user.Customer = new Customer();
        return user;
    }

    /// <summary>Creates a valid <see cref="Restaurant"/> for service tests.</summary>
    public static Restaurant CreateRestaurant(int id = 1, int ownerId = 5, decimal balance = 0m) => new()
    {
        Id = id,
        Name = "Test Restaurant",
        OwnerId = ownerId,
        Balance = balance,
        IsActive = true,
        AdressLine1 = "1 Rue Test",
        City = "Paris",
        ZipCode = "75001",
        Country = "FR"
    };

    /// <summary>Creates a <see cref="JwtConfig" /> suitable for test token generation.</summary>
    public static JwtConfig CreateTestJwtConfig() => new()
    {
        Key = "ThisIsATestSecretKeyThatIsLongEnoughForHmacSha256Signing!",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        ExpireMinutes = 30,
    };
}
