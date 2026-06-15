using DeliverTableServer.Common;
using DeliverTableServer.Features.Restaurant;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableTests.Global.Helpers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class RestaurantOrderConfigControllerTests
{
    private IRestaurantOrderConfigService _restaurantOrderConfigService = null!;
    private RestaurantOrderConfigController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _restaurantOrderConfigService = Substitute.For<IRestaurantOrderConfigService>();
        _sut = new RestaurantOrderConfigController(_restaurantOrderConfigService);
    }

    [Test]
    public async Task GetBlockedSlots_WhenAuthenticated_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "10", nameof(UserRole.RestaurantOwner));

        _restaurantOrderConfigService.GetBlockedSlotsAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminBlockedSlotResponse>>.Success(new List<AdminBlockedSlotResponse>()));

        IActionResult result = await _sut.GetBlockedSlots(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetBlockedSlots_WhenUnauthorized_ReturnsUnauthorized()
    {
        AuthenticationTestHelper.SetupUnauthenticatedUser(_sut);

        IActionResult result = await _sut.GetBlockedSlots(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task CreateBlockedSlot_WhenAuthenticated_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "10", nameof(UserRole.RestaurantOwner));

        AdminBlockedSlotResponse response = new AdminBlockedSlotResponse
        {
            Id = 5,
            RestaurantId = 1,
            RestaurantName = "Restaurant test"
        };

        _restaurantOrderConfigService.CreateBlockedSlotAsync(
                1,
                10,
                Arg.Any<AdminCreateBlockedSlotRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminBlockedSlotResponse>.Success(response));

        IActionResult result = await _sut.CreateBlockedSlot(1, new AdminCreateBlockedSlotRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task DeleteBlockedSlot_WhenAuthenticated_ReturnsNoContent()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "10", nameof(UserRole.RestaurantOwner));

        _restaurantOrderConfigService.DeleteBlockedSlotAsync(1, 5, 10, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        IActionResult result = await _sut.DeleteBlockedSlot(1, 5, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }
}
