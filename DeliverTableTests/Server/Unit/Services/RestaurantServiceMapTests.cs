using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class RestaurantServiceMapTests
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

    [Test]
    public async Task GetForMapAsync_ReturnsRestaurantMapDtos()
    {
        RestaurantQuery query = new RestaurantQuery { Latitude = 48.8, Longitude = 2.3, RadiusKm = 10 };
        List<Restaurant> restaurants = new List<Restaurant>
        {
            CreateRestaurant(id: 1),
            CreateRestaurant(id: 2)
        };
        restaurants[0].Latitude = 48.81;
        restaurants[0].Longitude = 2.31;
        restaurants[1].Latitude = 48.82;
        restaurants[1].Longitude = 2.32;

        _restaurantRepository.GetForMapAsync(query, Arg.Any<CancellationToken>()).Returns(restaurants);

        ServiceResult<List<RestaurantMapDto>> result = await _sut.GetForMapAsync(query);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
        Assert.That(result.Value![0].Id, Is.EqualTo(1));
        Assert.That(result.Value[0].Latitude, Is.EqualTo(48.81));
    }

    [Test]
    public async Task GetForMapAsync_WhenNoResults_ReturnsEmptyList()
    {
        RestaurantQuery query = new RestaurantQuery { Latitude = 48.8, Longitude = 2.3, RadiusKm = 1 };
        _restaurantRepository.GetForMapAsync(query, Arg.Any<CancellationToken>()).Returns(new List<Restaurant>());

        ServiceResult<List<RestaurantMapDto>> result = await _sut.GetForMapAsync(query);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty);
    }

    [Test]
    public async Task GetForMapAsync_MapsAllFields()
    {
        RestaurantQuery query = new RestaurantQuery { Latitude = 48.8, Longitude = 2.3, RadiusKm = 10 };
        Restaurant restaurant = CreateRestaurant(id: 42);
        restaurant.Name = "Le Gourmet";
        restaurant.Type = DeliverTableSharedLibrary.Enums.RestaurantType.Français;
        restaurant.Latitude = 48.85;
        restaurant.Longitude = 2.35;

        _restaurantRepository.GetForMapAsync(query, Arg.Any<CancellationToken>()).Returns(new List<Restaurant> { restaurant });

        ServiceResult<List<RestaurantMapDto>> result = await _sut.GetForMapAsync(query);

        Assert.That(result.IsSuccess, Is.True);
        RestaurantMapDto dto = result.Value![0];
        Assert.That(dto.Id, Is.EqualTo(42));
        Assert.That(dto.Name, Is.EqualTo("Le Gourmet"));
        Assert.That(dto.Type, Is.EqualTo("Français"));
        Assert.That(dto.Latitude, Is.EqualTo(48.85));
        Assert.That(dto.Longitude, Is.EqualTo(2.35));
    }
}
