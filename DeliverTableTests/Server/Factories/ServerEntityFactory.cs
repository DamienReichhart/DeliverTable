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

    /// <summary>Creates a valid <see cref="Cart"/> with two items for order tests.</summary>
    public static Cart CreateCart(int id = 1, int customerId = 1, int restaurantId = 1, decimal price1 = 10m, decimal price2 = 20m) => new()
    {
        Id = id,
        CustomerId = customerId,
        RestaurantId = restaurantId,
        Items =
        [
            new CartItem { DishId = 100, Dish = new Dish { Id = 100, Name = "Plat A" }, Quantity = 2, UnitPrice = price1 },
            new CartItem { DishId = 200, Dish = new Dish { Id = 200, Name = "Plat B" }, Quantity = 1, UnitPrice = price2 }
        ]
    };

    /// <summary>Creates a valid <see cref="Promotion"/> for service tests.</summary>
    public static Promotion CreatePromotion(
        int id = 1, int restaurantId = 1, PromotionType type = PromotionType.Automatic,
        DiscountType discountType = DiscountType.Percentage, decimal discountValue = 10m) => new()
    {
        Id = id,
        RestaurantId = restaurantId,
        Name = "Promotion Test",
        PromotionType = type,
        DiscountType = discountType,
        DiscountValue = discountValue,
        IsActive = true,
        StartsAt = DateTime.UtcNow.AddDays(-1),
        EndsAt = DateTime.UtcNow.AddDays(30),
        PromotionDishes = []
    };

    /// <summary>Creates a valid <see cref="DiscountCode"/> for service tests.</summary>
    public static DiscountCode CreateDiscountCode(
        int id = 1, int restaurantId = 1, string code = "SAVE10",
        DiscountType discountType = DiscountType.Percentage, decimal discountValue = 10m) => new()
    {
        Id = id,
        Code = code,
        RestaurantId = restaurantId,
        DiscountType = discountType,
        DiscountValue = discountValue,
        IsActive = true,
        ValidFrom = DateTime.UtcNow.AddDays(-1),
        ValidUntil = DateTime.UtcNow.AddDays(30),
        MaxRedemptions = 100,
        CurrentRedemptions = 0,
        PerUserLimit = 1
    };

    /// <summary>Creates a valid <see cref="LoyaltyProgram"/> for service tests.</summary>
    public static LoyaltyProgram CreateLoyaltyProgram(
        int id = 1, int restaurantId = 1, decimal pointsPerEuro = 1.0m, decimal eurosPerPoint = 0.10m) => new()
    {
        Id = id,
        RestaurantId = restaurantId,
        IsActive = true,
        PointsPerEuro = pointsPerEuro,
        EurosPerPoint = eurosPerPoint
    };

    /// <summary>Creates a valid <see cref="LoyaltyAccount"/> for service tests.</summary>
    public static LoyaltyAccount CreateLoyaltyAccount(
        int id = 1, int programId = 1, int customerId = 1, int pointsBalance = 0) => new()
    {
        Id = id,
        LoyaltyProgramId = programId,
        CustomerId = customerId,
        PointsBalance = pointsBalance
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
