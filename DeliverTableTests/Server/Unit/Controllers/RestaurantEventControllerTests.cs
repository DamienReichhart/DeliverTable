using DeliverTableServer.Common;
using DeliverTableServer.Features.Restaurant;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Event;
using DeliverTableTests.Global.Helpers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class RestaurantEventControllerTests
{
    private IRestaurantEventService _eventService = null!;
    private RestaurantEventController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _eventService = Substitute.For<IRestaurantEventService>();
        _sut = new RestaurantEventController(_eventService);
    }

    [Test]
    public async Task Create_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "5", nameof(UserRole.RestaurantOwner));
        var request = new CreateRestaurantEventRequest { Name = "Menu spécial Noël" };
        var dto = new RestaurantEventResponse { Id = 1, RestaurantId = 10, Name = "Menu spécial Noël" };
        _eventService.CreateAsync(10, 5, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RestaurantEventResponse>.Success(dto));

        var result = await _sut.Create(10, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Create_WhenUnauthorized_ReturnsUnauthorized()
    {
        AuthenticationTestHelper.SetupUnauthenticatedUser(_sut);

        var result = await _sut.Create(10, new CreateRestaurantEventRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task GetByRestaurant_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "5", nameof(UserRole.RestaurantOwner));
        _eventService.GetByRestaurantAsync(10, 5, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<RestaurantEventResponse>>.Success(
                [new RestaurantEventResponse { Id = 1, Name = "Noël" }]));

        var result = await _sut.GetByRestaurant(10, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetActiveByRestaurant_ReturnsOk_WhenAnonymous()
    {
        _eventService.GetActiveByRestaurantAsync(10, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<RestaurantEventResponse>>.Success(
                [new RestaurantEventResponse { Id = 1, Name = "Menu spécial Noël" }]));

        var result = await _sut.GetActiveByRestaurant(10, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Update_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "5", nameof(UserRole.RestaurantOwner));
        var request = new UpdateRestaurantEventRequest { Name = "Menu spécial Noël" };
        _eventService.UpdateAsync(1, 5, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RestaurantEventResponse>.Success(
                new RestaurantEventResponse { Id = 1, Name = "Menu spécial Noël" }));

        var result = await _sut.Update(1, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Delete_ReturnsNoContent()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "5", nameof(UserRole.RestaurantOwner));
        _eventService.DeleteAsync(1, 5, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_WhenServiceFails_ReturnsError()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "5", nameof(UserRole.RestaurantOwner));
        _eventService.DeleteAsync(1, 5, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure(new ServiceError("Événement introuvable", 404)));

        var result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(404));
    }
}
