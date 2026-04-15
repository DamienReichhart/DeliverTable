using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dispute;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Helpers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class DisputeControllerTests
{
    private IDisputeService _service = null!;
    private DisputeController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IDisputeService>();
        _sut = new DisputeController(_service);
    }

    [Test]
    public async Task GetForRestaurant_Owner_ReturnsPaginatedList()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "7", nameof(UserRole.RestaurantOwner));
        var paginated = new PaginatedResult<DisputeRowDto>
        {
            Items =
            [
                new DisputeRowDto(1, "dp_1", 10, 25m, "EUR", "fraudulent",
                    DisputeState.Open, DateTime.UtcNow, null, DateTime.UtcNow.AddDays(7)),
            ],
            TotalCount = 1,
            Page = 1,
            PageSize = 20,
        };
        _service.ListForRestaurantAsync(5, 1, 20, 7, false, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<DisputeRowDto>>.Success(paginated));

        var result = await _sut.GetForRestaurant(5, 1, 20, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.InstanceOf<PaginatedResult<DisputeRowDto>>());
    }

    [Test]
    public async Task GetForRestaurant_Admin_PassesIsAdminTrue()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "99", nameof(UserRole.Administrator));
        _service.ListForRestaurantAsync(5, 1, 20, 99, true, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<DisputeRowDto>>.Success(
                new PaginatedResult<DisputeRowDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 }));

        var result = await _sut.GetForRestaurant(5, 1, 20, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        await _service.Received(1).ListForRestaurantAsync(
            5, 1, 20, 99, true, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetForRestaurant_AccessDenied_ReturnsError()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "7", nameof(UserRole.RestaurantOwner));
        _service.ListForRestaurantAsync(5, 1, 20, 7, false, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<DisputeRowDto>>.Failure(
                new ServiceError("Denied", 403)));

        var result = await _sut.GetForRestaurant(5, 1, 20, CancellationToken.None);

        Assert.That(result, Is.Not.InstanceOf<OkObjectResult>());
    }
}
