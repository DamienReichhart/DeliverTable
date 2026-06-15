using DeliverTableServer.Common;
using DeliverTableServer.Features.Restaurant;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class RestaurantControllerMapTests
{
    private IRestaurantService _restaurantService = null!;
    private RestaurantController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _restaurantService = Substitute.For<IRestaurantService>();
        _sut = new RestaurantController(_restaurantService);
    }

    [Test]
    public async Task GetForMap_ReturnsOkWithRestaurantList()
    {
        RestaurantQuery query = new RestaurantQuery { Latitude = 48.8, Longitude = 2.3, RadiusKm = 10 };
        List<RestaurantMapDto> restaurants = new List<RestaurantMapDto>
        {
            new(1, "Resto A", "Français", 48.81, 2.31),
            new(2, "Resto B", "Italien", 48.82, 2.32)
        };
        _restaurantService.GetForMapAsync(query, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<RestaurantMapDto>>.Success(restaurants));

        IActionResult result = await _sut.GetForMap(query, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        OkObjectResult ok = (OkObjectResult)result;
        List<RestaurantMapDto>? value = ok.Value as List<RestaurantMapDto>;
        Assert.That(value, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetForMap_WhenEmpty_ReturnsOkWithEmptyList()
    {
        RestaurantQuery query = new RestaurantQuery { Latitude = 48.8, Longitude = 2.3, RadiusKm = 1 };
        _restaurantService.GetForMapAsync(query, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<RestaurantMapDto>>.Success(new List<RestaurantMapDto>()));

        IActionResult result = await _sut.GetForMap(query, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        OkObjectResult ok = (OkObjectResult)result;
        List<RestaurantMapDto>? value = ok.Value as List<RestaurantMapDto>;
        Assert.That(value, Is.Empty);
    }
}
