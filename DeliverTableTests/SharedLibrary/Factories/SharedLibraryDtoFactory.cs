using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Auth;

namespace DeliverTableTests.SharedLibrary.Factories;

/// <summary>
///     Factory methods that produce valid DTO instances.
///     Tests start from a known-good state and mutate only the field under test,
///     isolating each validation rule.
/// </summary>
public static class SharedLibraryDtoFactory
{
    public static LoginRequest CreateValidLoginRequest() => new()
    {
        Email = "user@example.com",
        Password = "SecurePass123!"
    };

    public static RegisterRequest CreateValidRegisterRequest() => new()
    {
        FirstName = "Jean",
        LastName = "Dupont",
        Email = "jean.dupont@example.com",
        Password = "SecurePass123!",
        ConfirmPassword = "SecurePass123!"
    };

    public static RestaurantRegister CreateValidRestaurantRegister() => new()
    {
        FirstName = "Marie",
        LastName = "Curie",
        CompanyName = "Le Bon Restaurant",
        VatNumber = "BE0123456789",
        ContactPhoneNumber = "+32470123456",
        Email = "contact@restaurant.be",
        Password = "SecurePass123!",
        ConfirmPassword = "SecurePass123!"
    };

    public static HealthResponse CreateHealthResponse(
        string status = "Healthy",
        DateTime? timestampUtc = null) => new()
    {
        Status = status,
        TimestampUtc = timestampUtc ?? DateTime.UtcNow
    };
}
