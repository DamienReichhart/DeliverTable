using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Auth;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Server.Factories;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class AuthServiceTests
{
    private IUserRepository _userRepository = null!;
    private ITokenService _tokenService = null!;
    private IEmailJobService _emailJobService = null!;
    private IRestaurantService _restaurantService = null!;
    private AuthService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _tokenService = Substitute.For<ITokenService>();
        _emailJobService = Substitute.For<IEmailJobService>();
        _restaurantService = Substitute.For<IRestaurantService>();
        _sut = new AuthService(_userRepository, _tokenService, _emailJobService, _restaurantService);
    }

    [Test]
    public async Task UpdateProfile_WithBillingFields_PersistsThemTrimmed()
    {
        User user = ServerEntityFactory.CreateValidUser("u@example.fr");
        user.Id = 42;
        user.BillingAddressLine1 = string.Empty;
        user.BillingPostalCode = string.Empty;
        user.BillingCity = string.Empty;
        user.BillingCountry = string.Empty;

        _userRepository.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(user);
        _userRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        UpdateProfileRequest request = new UpdateProfileRequest
        {
            FirstName = "Jean",
            LastName = "Dupont",
            Email = "u@example.fr",
            BillingAddressLine1 = "  12 rue de la Paix  ",
            BillingAddressLine2 = "",
            BillingPostalCode = " 75002 ",
            BillingCity = " Paris ",
            BillingCountry = " France ",
        };

        ServiceResult<ConnectionResponse> result = await _sut.UpdateProfileAsync(42, request, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(user.BillingAddressLine1, Is.EqualTo("12 rue de la Paix"));
        Assert.That(user.BillingAddressLine2, Is.EqualTo(""));
        Assert.That(user.BillingPostalCode, Is.EqualTo("75002"));
        Assert.That(user.BillingCity, Is.EqualTo("Paris"));
        Assert.That(user.BillingCountry, Is.EqualTo("France"));
    }

    #region RegisterRestaurantAsync

    [Test]
    public async Task RegisterRestaurantAsync_EmailAlreadyUsed_ReturnsError_NoUserCreated()
    {
        RestaurantRegister request = BuildValidRestaurantRegister();
        _userRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        ServiceResult<ConnectionResponse> result = await _sut.RegisterRestaurantAsync(request, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.EmailAlreadyUsed));
        await _userRepository.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<string>());
        await _restaurantService.DidNotReceive().ValidateLegalAndLocateAsync(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task RegisterRestaurantAsync_RestaurantValidationFails_NoUserCreated()
    {
        RestaurantRegister request = BuildValidRestaurantRegister();
        _userRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _restaurantService.ValidateLegalAndLocateAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new ServiceError(ErrorMessages.SiretInvalid));

        ServiceResult<ConnectionResponse> result = await _sut.RegisterRestaurantAsync(request, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.SiretInvalid));
        await _userRepository.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<string>());
    }

    [Test]
    public async Task RegisterRestaurantAsync_ValidPayload_CreatesUserOwnerRoleAndRestaurant()
    {
        RestaurantRegister request = BuildValidRestaurantRegister();
        _userRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _restaurantService.ValidateLegalAndLocateAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((48.85, 2.35));
        _userRepository.CreateAsync(Arg.Any<User>(), Arg.Any<string>())
            .Returns((true, Enumerable.Empty<string>()));
        _userRepository.AddToRoleAsync(Arg.Any<User>(), nameof(UserRole.RestaurantOwner))
            .Returns((true, Enumerable.Empty<string>()));
        _restaurantService.CreateValidatedAsync(
                Arg.Any<CreateRestaurantDto>(), Arg.Any<int>(),
                Arg.Any<(double lat, double lon)>(), Arg.Any<CancellationToken>())
            .Returns(new RestaurantDto { Id = 99, Name = "Test" });
        _userRepository.GetPrimaryRoleAsync(Arg.Any<User>())
            .Returns(nameof(UserRole.RestaurantOwner));
        _tokenService.CreateToken(Arg.Any<User>())
            .Returns("test-token");

        ServiceResult<ConnectionResponse> result = await _sut.RegisterRestaurantAsync(request, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _userRepository.Received(1).CreateAsync(Arg.Any<User>(), request.Password);
        await _userRepository.Received(1).AddToRoleAsync(Arg.Any<User>(), nameof(UserRole.RestaurantOwner));
        await _restaurantService.Received(1).CreateValidatedAsync(
            request.Restaurant, Arg.Any<int>(),
            Arg.Is<(double lat, double lon)>(c => c.lat == 48.85 && c.lon == 2.35),
            Arg.Any<CancellationToken>());
        await _emailJobService.Received(1).QueueWelcomeEmailAsync(request.Email, Arg.Any<string>());
    }

    [Test]
    public async Task RegisterRestaurantAsync_RestaurantInsertFails_DeletesUser()
    {
        RestaurantRegister request = BuildValidRestaurantRegister();
        _userRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _restaurantService.ValidateLegalAndLocateAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((48.85, 2.35));
        _userRepository.CreateAsync(Arg.Any<User>(), Arg.Any<string>())
            .Returns((true, Enumerable.Empty<string>()));
        _userRepository.AddToRoleAsync(Arg.Any<User>(), nameof(UserRole.RestaurantOwner))
            .Returns((true, Enumerable.Empty<string>()));
        _restaurantService.CreateValidatedAsync(
                Arg.Any<CreateRestaurantDto>(), Arg.Any<int>(),
                Arg.Any<(double lat, double lon)>(), Arg.Any<CancellationToken>())
            .Returns(new ServiceError(ErrorMessages.InternalError, 500));
        _userRepository.DeleteAsync(Arg.Any<User>())
            .Returns((true, Enumerable.Empty<string>()));

        ServiceResult<ConnectionResponse> result = await _sut.RegisterRestaurantAsync(request, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        await _userRepository.Received(1).DeleteAsync(Arg.Any<User>());
        await _emailJobService.DidNotReceive().QueueWelcomeEmailAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task RegisterRestaurantAsync_RoleAssignmentFails_DeletesUser()
    {
        RestaurantRegister request = BuildValidRestaurantRegister();
        _userRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _restaurantService.ValidateLegalAndLocateAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((48.85, 2.35));
        _userRepository.CreateAsync(Arg.Any<User>(), Arg.Any<string>())
            .Returns((true, Enumerable.Empty<string>()));
        _userRepository.AddToRoleAsync(Arg.Any<User>(), nameof(UserRole.RestaurantOwner))
            .Returns((false, new[] { "role error" }));
        _userRepository.DeleteAsync(Arg.Any<User>())
            .Returns((true, Enumerable.Empty<string>()));

        ServiceResult<ConnectionResponse> result = await _sut.RegisterRestaurantAsync(request, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        await _userRepository.Received(1).DeleteAsync(Arg.Any<User>());
        await _restaurantService.DidNotReceive().CreateValidatedAsync(
            Arg.Any<CreateRestaurantDto>(), Arg.Any<int>(),
            Arg.Any<(double lat, double lon)>(), Arg.Any<CancellationToken>());
        await _emailJobService.DidNotReceive().QueueWelcomeEmailAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    private static RestaurantRegister BuildValidRestaurantRegister() => new()
    {
        FirstName = "Jean",
        LastName = "Dupont",
        Email = "owner@example.com",
        ContactPhoneNumber = "+33612345678",
        Password = "SecurePass1234!",
        ConfirmPassword = "SecurePass1234!",
        Restaurant = new CreateRestaurantDto
        {
            Name = "Le Bon Resto",
            Description = "Une description",
            AdressLine1 = "1 rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = AvailableCountries.France.ToString(),
            Type = RestaurantType.Autre.ToString(),
            Siret = "73282932000074",
            LegalName = "Le Bon Resto SAS",
            LegalAddress = "1 rue Test",
            LegalForm = "SAS",
            IsVatRegistered = true,
            VatNumber = "FR12345678901",
        }
    };
}
