using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Auth;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using DeliverTableSharedLibrary.Enums;

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
        ContactPhoneNumber = "+32470123456",
        Email = "contact@restaurant.be",
        Password = "SecurePass123!",
        ConfirmPassword = "SecurePass123!",
        Siret = "78467169500103",
        Restaurant = new CreateRestaurantDto
        {
            Name = "Le Bon Restaurant",
            Description = "Une description",
            AdressLine1 = "1 rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = AvailableCountries.France.ToString(),
            Type = RestaurantType.Autre.ToString(),
            Siret = "78467169500103",
            LegalName = "Le Bon Restaurant SAS",
            LegalAddress = "1 rue Test",
            LegalForm = "SAS",
            IsVatRegistered = true,
            VatNumber = "FR12345678901",
        }
    };

    public static HealthResponse CreateHealthResponse(
        string status = nameof(HealthStatus.Healthy),
        DateTime? timestampUtc = null) => new()
        {
            Status = status,
            TimestampUtc = timestampUtc ?? DateTime.UtcNow
        };
}
