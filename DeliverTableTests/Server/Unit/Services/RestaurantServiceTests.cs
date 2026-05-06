using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class RestaurantServiceTests
{
    private IRestaurantRepository _restaurantRepository = null!;
    private IGeoLocationService _geoLocationService = null!;
    private RestaurantService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _geoLocationService = Substitute.For<IGeoLocationService>();
        _sut = new RestaurantService(_restaurantRepository, _geoLocationService);
    }

    #region CreateAsync - SIRET validation

    [Test]
    public async Task CreateAsync_InvalidSiret_ReturnsError()
    {
        var request = new CreateRestaurantDto
        {
            Name = "X",
            Description = "Une description",
            AdressLine1 = "1 rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "France",
            Type = RestaurantType.Autre.ToString(),
            Siret = "12345678900012",
            LegalName = "X SAS",
            LegalAddress = "1 rue",
            LegalForm = "SAS",
            IsVatRegistered = true,
        };

        var result = await _sut.CreateAsync(request, ownerId: 1, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.SiretInvalid));
    }

    [Test]
    public async Task CreateAsync_MissingLegalFields_ReturnsError()
    {
        var request = new CreateRestaurantDto
        {
            Name = "X",
            Description = "Une description",
            AdressLine1 = "1 rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "France",
            Type = RestaurantType.Autre.ToString(),
            Siret = "73282932000074",
            LegalName = "",
            LegalAddress = "1 rue",
            LegalForm = "SAS",
            IsVatRegistered = true,
        };

        var result = await _sut.CreateAsync(request, ownerId: 1, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.LegalFieldsRequired));
    }

    [Test]
    public async Task CreateAsync_ValidLegalData_Persists()
    {
        var request = new CreateRestaurantDto
        {
            Name = "Valid Restaurant",
            Description = "Une description valide",
            AdressLine1 = "1 rue Valid",
            City = "Paris",
            ZipCode = "75001",
            Country = "France",
            Type = RestaurantType.Autre.ToString(),
            Siret = "73282932000074",
            LegalName = "Valid SAS",
            LegalAddress = "1 rue Valid",
            LegalForm = "SAS",
            IsVatRegistered = true,
            VatNumber = "FR12345678901",
        };

        _geoLocationService
            .GetCoordinatesAsync(request.AdressLine1, request.City, request.ZipCode)
            .Returns((48.85, 2.35));

        var createdRestaurant = CreateRestaurant(id: 99, ownerId: 1);
        createdRestaurant.Siret = request.Siret;
        createdRestaurant.LegalName = request.LegalName;
        createdRestaurant.LegalAddress = request.LegalAddress;
        createdRestaurant.LegalForm = request.LegalForm;
        createdRestaurant.IsVatRegistered = request.IsVatRegistered;

        _restaurantRepository
            .CreateAsync(Arg.Any<Restaurant>(), Arg.Any<CancellationToken>())
            .Returns(createdRestaurant);

        var result = await _sut.CreateAsync(request, ownerId: 1, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _restaurantRepository.Received(1).CreateAsync(
            Arg.Is<Restaurant>(r =>
                r.Siret == "73282932000074"
                && r.LegalName == "Valid SAS"
                && r.LegalAddress == "1 rue Valid"
                && r.LegalForm == "SAS"
                && r.IsVatRegistered == true),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateAsync_IsVatRegisteredTrueWithoutVatNumber_ReturnsError()
    {
        var request = new CreateRestaurantDto
        {
            Name = "X",
            Description = "Une description",
            AdressLine1 = "1 rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "France",
            Type = RestaurantType.Autre.ToString(),
            Siret = "73282932000074",
            LegalName = "X SAS",
            LegalAddress = "1 rue",
            LegalForm = "SAS",
            IsVatRegistered = true,
            VatNumber = null,
        };

        _geoLocationService
            .GetCoordinatesAsync(request.AdressLine1, request.City, request.ZipCode)
            .Returns((48.85, 2.35));

        var result = await _sut.CreateAsync(request, ownerId: 1, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.VatNumberRequiredWhenVatRegistered));
    }

    [Test]
    public async Task CreateAsync_IsVatRegisteredTrueWithVatNumber_PersistsVatNumber()
    {
        var request = new CreateRestaurantDto
        {
            Name = "Valid Restaurant",
            Description = "Une description",
            AdressLine1 = "1 rue Valid",
            City = "Paris",
            ZipCode = "75001",
            Country = "France",
            Type = RestaurantType.Autre.ToString(),
            Siret = "73282932000074",
            LegalName = "Valid SAS",
            LegalAddress = "1 rue Valid",
            LegalForm = "SAS",
            IsVatRegistered = true,
            VatNumber = "FR12345678901",
        };

        _geoLocationService
            .GetCoordinatesAsync(request.AdressLine1, request.City, request.ZipCode)
            .Returns((48.85, 2.35));

        var createdRestaurant = CreateRestaurant(id: 99, ownerId: 1);
        _restaurantRepository
            .CreateAsync(Arg.Any<Restaurant>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Restaurant>());

        var result = await _sut.CreateAsync(request, ownerId: 1, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _restaurantRepository.Received(1).CreateAsync(
            Arg.Is<Restaurant>(r =>
                r.IsVatRegistered == true
                && r.VatNumber == "FR12345678901"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateAsync_IsVatRegisteredFalseWithStaleVatNumber_PersistsAsNull()
    {
        var request = new CreateRestaurantDto
        {
            Name = "Valid Restaurant",
            Description = "Une description",
            AdressLine1 = "1 rue Valid",
            City = "Paris",
            ZipCode = "75001",
            Country = "France",
            Type = RestaurantType.Autre.ToString(),
            Siret = "73282932000074",
            LegalName = "Valid SAS",
            LegalAddress = "1 rue Valid",
            LegalForm = "SAS",
            IsVatRegistered = false,
            VatNumber = "FR12345678901",
        };

        _geoLocationService
            .GetCoordinatesAsync(request.AdressLine1, request.City, request.ZipCode)
            .Returns((48.85, 2.35));

        _restaurantRepository
            .CreateAsync(Arg.Any<Restaurant>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Restaurant>());

        var result = await _sut.CreateAsync(request, ownerId: 1, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _restaurantRepository.Received(1).CreateAsync(
            Arg.Is<Restaurant>(r =>
                r.IsVatRegistered == false
                && r.VatNumber == null),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region UpdateAsync - SIRET validation

    [Test]
    public async Task UpdateAsync_InvalidSiret_ReturnsError()
    {
        var existing = CreateRestaurant(id: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var request = new UpdateRestaurantDto
        {
            Name = "X",
            Description = "Une description",
            AdressLine1 = "1 rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "France",
            Type = RestaurantType.Autre.ToString(),
            Siret = "12345678900012",
            LegalName = "X SAS",
            LegalAddress = "1 rue",
            LegalForm = "SAS",
            IsVatRegistered = true,
        };

        var result = await _sut.UpdateAsync(1, request, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.SiretInvalid));
    }

    [Test]
    public async Task UpdateAsync_MissingLegalFields_ReturnsError()
    {
        var existing = CreateRestaurant(id: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var request = new UpdateRestaurantDto
        {
            Name = "X",
            Description = "Une description",
            AdressLine1 = "1 rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "France",
            Type = RestaurantType.Autre.ToString(),
            Siret = "73282932000074",
            LegalName = "X SAS",
            LegalAddress = "",
            LegalForm = "SAS",
            IsVatRegistered = false,
        };

        var result = await _sut.UpdateAsync(1, request, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.LegalFieldsRequired));
    }

    [Test]
    public async Task UpdateAsync_ValidLegalData_Persists()
    {
        var existing = CreateRestaurant(id: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var request = new UpdateRestaurantDto
        {
            Name = "Updated Restaurant",
            Description = "Une description mise à jour",
            AdressLine1 = "1 rue Updated",
            City = "Lyon",
            ZipCode = "69001",
            Country = "France",
            Type = RestaurantType.Autre.ToString(),
            Siret = "73282932000074",
            LegalName = "Updated SAS",
            LegalAddress = "1 rue Updated",
            LegalForm = "SAS",
            IsVatRegistered = false,
        };

        _geoLocationService
            .GetCoordinatesAsync(request.AdressLine1, request.City, request.ZipCode)
            .Returns((45.75, 4.83));

        _restaurantRepository
            .UpdateAsync(Arg.Any<Restaurant>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Restaurant>());

        var result = await _sut.UpdateAsync(1, request, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _restaurantRepository.Received(1).UpdateAsync(
            Arg.Is<Restaurant>(r =>
                r.Siret == "73282932000074"
                && r.LegalName == "Updated SAS"
                && r.LegalAddress == "1 rue Updated"
                && r.LegalForm == "SAS"
                && r.IsVatRegistered == false),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
