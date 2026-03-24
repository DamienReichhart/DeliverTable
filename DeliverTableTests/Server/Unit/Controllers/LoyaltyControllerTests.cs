using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Loyalty;
using DeliverTableTests.Global.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class LoyaltyControllerTests
{
    private ILoyaltyService _loyaltyService = null!;
    private LoyaltyController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _loyaltyService = Substitute.For<ILoyaltyService>();
        _sut = new LoyaltyController(_loyaltyService);
    }

    private void SetupAuthenticatedUser(string userId, string role = nameof(UserRole.RestaurantOwner))
        => AuthenticationTestHelper.SetupAuthenticatedUser(_sut, userId, role);

    [Test]
    public async Task CreateOrUpdate_ReturnsOk()
    {
        SetupAuthenticatedUser("5");
        var request = new CreateLoyaltyProgramRequest();
        var dto = new LoyaltyProgramDto { Id = 1, RestaurantId = 10, PointsPerEuro = 1m, EurosPerPoint = 0.01m, IsActive = true };
        _loyaltyService.CreateOrUpdateProgramAsync(10, 5, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<LoyaltyProgramDto>.Success(dto));

        var result = await _sut.CreateOrUpdate(10, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetProgram_ReturnsOk()
    {
        var dto = new LoyaltyProgramDto { Id = 1, RestaurantId = 10, PointsPerEuro = 1m, EurosPerPoint = 0.01m, IsActive = true };
        _loyaltyService.GetProgramAsync(10, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<LoyaltyProgramDto>.Success(dto));

        var result = await _sut.GetProgram(10, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetMyAccount_ReturnsOk()
    {
        SetupAuthenticatedUser("5", nameof(UserRole.Customer));
        var dto = new LoyaltyAccountDto { Id = 1, PointsBalance = 100, EuroEquivalent = 1m, PointsPerEuro = 1m, EurosPerPoint = 0.01m };
        _loyaltyService.GetMyAccountAsync(10, 5, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<LoyaltyAccountDto>.Success(dto));

        var result = await _sut.GetMyAccount(10, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetMyAccount_WhenUnauthorized_ReturnsUnauthorized()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _sut.GetMyAccount(10, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }
}
