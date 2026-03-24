using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using DeliverTableTests.Global.Helpers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class RestaurantControllerTests
{
    private IRestaurantService _restaurantService = null!;
    private RestaurantController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _restaurantService = Substitute.For<IRestaurantService>();
        _sut = new RestaurantController(_restaurantService);
    }

    private void SetupAuthenticatedUser(string userId, string role = nameof(UserRole.RestaurantOwner))
        => AuthenticationTestHelper.SetupAuthenticatedUser(_sut, userId, role);

    [Test]
    public async Task GetAll_ReturnsOkWithPaginatedResult()
    {
        var query = new RestaurantQuery();
        var paginated = new PaginatedResult<RestaurantDto>
        {
            Items = [new RestaurantDto { Id = 1, Name = "Resto 1" }, new RestaurantDto { Id = 2, Name = "Resto 2" }],
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };
        _restaurantService.GetAllAsync(query, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<RestaurantDto>>.Success(paginated));

        var result = await _sut.GetAll(query, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAllUserRestaurants_WithValidOwnerId_ReturnsOk()
    {
        var userId = 5;
        SetupAuthenticatedUser(userId.ToString());
        var query = new RestaurantQuery();
        var paginated = new PaginatedResult<RestaurantDto>
        {
            Items = [new RestaurantDto { Id = 1, Name = "My Resto" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };
        _restaurantService.GetByOwnerAsync(userId, query, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<RestaurantDto>>.Success(paginated));

        var result = await _sut.GetAllUserRestaurants(query, CancellationToken.None, null);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAllUserRestaurants_WithMismatchedUserId_ReturnsForbid()
    {
        SetupAuthenticatedUser("5", role: "User");
        var query = new RestaurantQuery();

        var result = await _sut.GetAllUserRestaurants(query, CancellationToken.None, 10);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetById_WhenExists_ReturnsOk()
    {
        var dto = new DetailedRestaurantDto { Id = 1, Name = "Test Resto" };
        _restaurantService.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DetailedRestaurantDto>.Success(dto));

        var result = await _sut.GetById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_WhenNotExists_ReturnsError()
    {
        _restaurantService.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DetailedRestaurantDto>.Failure(new ServiceError("Not found", 404)));

        var result = await _sut.GetById(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        _restaurantService.DeleteAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }
}
