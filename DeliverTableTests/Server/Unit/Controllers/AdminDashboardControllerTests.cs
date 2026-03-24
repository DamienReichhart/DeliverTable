using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminDashboardControllerTests
{
    private IAdminDashboardService _adminDashboardService = null!;
    private AdminDashboardController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _adminDashboardService = Substitute.For<IAdminDashboardService>();
        _sut = new AdminDashboardController(_adminDashboardService);
    }

    [Test]
    public async Task GetStats_ReturnsOk()
    {
        var stats = new AdminDashboardStatsResponse
        {
            TotalUsers = 100,
            TotalRestaurants = 25,
            TotalOrders = 500,
            TotalRevenue = 12345.67m,
            ActivePromotions = 10,
            PendingOrders = 15
        };
        _adminDashboardService.GetStatsAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminDashboardStatsResponse>.Success(stats));

        var result = await _sut.GetStats(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.SameAs(stats));
    }

    [Test]
    public async Task GetStats_WhenError_ReturnsError()
    {
        _adminDashboardService.GetStatsAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminDashboardStatsResponse>.Failure(new ServiceError("Erreur", 500)));

        var result = await _sut.GetStats(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(500));
    }
}
