using DeliverTableServer.Common;
using DeliverTableServer.Features.Loyalty;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Loyalty;
using DeliverTableTests.Global.Helpers;
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

    [Test]
    public async Task CreateOrUpdate_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "5", nameof(UserRole.RestaurantOwner));
        CreateLoyaltyProgramRequest request = new CreateLoyaltyProgramRequest();
        LoyaltyProgramDto dto = new LoyaltyProgramDto { Id = 1, RestaurantId = 10, PointsPerEuro = 1m, EurosPerPoint = 0.01m, IsActive = true };
        _loyaltyService.CreateOrUpdateProgramAsync(10, 5, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<LoyaltyProgramDto>.Success(dto));

        IActionResult result = await _sut.CreateOrUpdate(10, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetProgram_ReturnsOk()
    {
        LoyaltyProgramDto dto = new LoyaltyProgramDto { Id = 1, RestaurantId = 10, PointsPerEuro = 1m, EurosPerPoint = 0.01m, IsActive = true };
        _loyaltyService.GetProgramAsync(10, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<LoyaltyProgramDto>.Success(dto));

        IActionResult result = await _sut.GetProgram(10, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetMyAccount_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "5", nameof(UserRole.Customer));
        LoyaltyAccountDto dto = new LoyaltyAccountDto { Id = 1, PointsBalance = 100, EuroEquivalent = 1m, PointsPerEuro = 1m, EurosPerPoint = 0.01m };
        _loyaltyService.GetMyAccountAsync(10, 5, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<LoyaltyAccountDto>.Success(dto));

        IActionResult result = await _sut.GetMyAccount(10, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetMyAccount_WhenUnauthorized_ReturnsUnauthorized()
    {
        AuthenticationTestHelper.SetupUnauthenticatedUser(_sut);

        IActionResult result = await _sut.GetMyAccount(10, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }
}
