using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DeliverTableServer.Controllers;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class RestaurantControllerTests
{
    private IGeoLocationService _geoLocationService = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private RestaurantController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _geoLocationService = Substitute.For<IGeoLocationService>();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _sut = new RestaurantController(_geoLocationService, _restaurantRepository);
    }

    private void SetupAuthenticatedUser(string userId, string role = "RestaurantOwner")
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(userId))
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        if (!string.IsNullOrEmpty(role))
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Test]
    public async Task GetAll_ReturnsOkWithRestaurants()
    {
        // Arrange
        var query = new RestaurantQuery();
        var restaurants = new List<Restaurant>
        {
            new Restaurant { Id = 1, Name = "Resto 1" },
            new Restaurant { Id = 2, Name = "Resto 2" }
        };
        _restaurantRepository.GetAllRestaurant(query).Returns(restaurants);

        // Act
        var result = await _sut.GetAll(query);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        var returnedRestaurants = okResult.Value as IEnumerable<RestaurantDto>;
        Assert.That(returnedRestaurants, Is.Not.Null);
        Assert.That(returnedRestaurants!.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetAllUserRestaurants_WithValidOwnerId_ReturnsOk()
    {
        // Arrange
        var userId = 5;
        SetupAuthenticatedUser(userId.ToString());
        var query = new RestaurantQuery();
        var restaurants = new List<Restaurant>
        {
            new Restaurant { Id = 1, Name = "My Resto", OwnerId = userId }
        };
        _restaurantRepository.GetRestaurantByOwner(userId, query).Returns(restaurants);

        // Act
        var result = await _sut.GetAllUserRestaurants(query, null); // id null triggers falling back to claim

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        var returnedRestaurants = okResult.Value as IEnumerable<RestaurantDto>;
        Assert.That(returnedRestaurants, Is.Not.Null);
        Assert.That(returnedRestaurants!.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetAllUserRestaurants_WithMismatchedUserId_ReturnsForbid()
    {
        // Arrange
        var userId = 5;
        SetupAuthenticatedUser(userId.ToString(), role: "User"); // Not Admin
        var query = new RestaurantQuery();
        var requestedOwnerId = 10;

        // Act
        var result = await _sut.GetAllUserRestaurants(query, requestedOwnerId);

        // Assert
        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetById_WhenExists_ReturnsOkWithDetailedDto()
    {
        // Arrange
        var restoId = 1;
        var restaurant = new Restaurant { Id = restoId, Name = "Test Resto" };
        _restaurantRepository.GetRestaurantById(restoId).Returns(restaurant);

        // Act
        var result = await _sut.GetById(restoId);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        var dto = okResult.Value as DetailedRestaurantDto;
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.Id, Is.EqualTo(restoId));
    }

    [Test]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var restoId = 99;
        _restaurantRepository.GetRestaurantById(restoId).Returns((Restaurant)null!);

        // Act
        var result = await _sut.GetById(restoId);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Update_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var restoId = 1;
        var dto = new UpdateRestaurantDto();
        _sut.ModelState.AddModelError("Name", "Required");

        // Act
        var result = await _sut.Update(restoId, dto);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Update_WithInvalidCoordinates_ReturnsBadRequest()
    {
        // Arrange
        var restoId = 1;
        var dto = new UpdateRestaurantDto { AdressLine1 = "123 Fake St", City = "FakeTown", ZipCode = "00000" };
        _geoLocationService.GetCoordinatesAsync(dto.AdressLine1, dto.City, dto.ZipCode).Returns(((double lat, double lon)?)null);

        // Act
        var result = await _sut.Update(restoId, dto);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Update_WithValidData_ReturnsOk()
    {
        // Arrange
        var restoId = 1;
        var dto = new UpdateRestaurantDto { AdressLine1 = "123 Real St", City = "RealTown", ZipCode = "12345" };
        _geoLocationService.GetCoordinatesAsync(dto.AdressLine1, dto.City, dto.ZipCode).Returns((lat: 45.0, lon: 4.0));

        var updatedResto = new Restaurant { Id = restoId, Name = "Real Resto" };
        _restaurantRepository.GetRestaurantById(restoId).Returns(updatedResto);

        // Act
        var result = await _sut.Update(restoId, dto);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        var returnedResto = okResult.Value as DetailedRestaurantDto;
        Assert.That(returnedResto, Is.Not.Null);
        Assert.That(returnedResto!.Id, Is.EqualTo(restoId));
    }

    [Test]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        // Arrange
        var restoId = 1;
        _restaurantRepository.Delete(restoId).Returns(true);

        // Act
        var result = await _sut.Delete(restoId);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_WhenFailed_ReturnsNotFound()
    {
        // Arrange
        var restoId = 99;
        _restaurantRepository.Delete(restoId).Returns(false);

        // Act
        var result = await _sut.Delete(restoId);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}
