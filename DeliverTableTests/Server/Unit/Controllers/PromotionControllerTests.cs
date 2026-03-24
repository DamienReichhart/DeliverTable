using System.Security.Claims;
using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Promotion;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class PromotionControllerTests
{
    private IPromotionService _promotionService = null!;
    private PromotionController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _promotionService = Substitute.For<IPromotionService>();
        _sut = new PromotionController(_promotionService);
    }

    private void SetupAuthenticatedUser(string userId, string role = nameof(UserRole.RestaurantOwner))
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(userId))
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        if (!string.IsNullOrEmpty(role))
            claims.Add(new Claim(ClaimTypes.Role, role));
        var identity = new ClaimsIdentity(claims, "TestScheme");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Test]
    public async Task Create_ReturnsOk()
    {
        SetupAuthenticatedUser("5");
        var request = new CreatePromotionRequest();
        var dto = new PromotionDto { Id = 1, RestaurantId = 10, Name = "Promo été" };
        _promotionService.CreateAsync(10, 5, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PromotionDto>.Success(dto));

        var result = await _sut.Create(10, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Create_WhenUnauthorized_ReturnsUnauthorized()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _sut.Create(10, new CreatePromotionRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task GetByRestaurant_ReturnsOk()
    {
        SetupAuthenticatedUser("5");
        var query = new PromotionQuery();
        var paginated = new PaginatedResult<PromotionDto>
        {
            Items = [new PromotionDto { Id = 1, Name = "Promo" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };
        _promotionService.GetByRestaurantAsync(10, 5, query, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<PromotionDto>>.Success(paginated));

        var result = await _sut.GetByRestaurant(10, query, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Update_ReturnsOk()
    {
        SetupAuthenticatedUser("5");
        var request = new UpdatePromotionRequest();
        var dto = new PromotionDto { Id = 1, Name = "Promo mise à jour" };
        _promotionService.UpdateAsync(1, 5, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PromotionDto>.Success(dto));

        var result = await _sut.Update(1, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Delete_ReturnsNoContent()
    {
        SetupAuthenticatedUser("5");
        _promotionService.DeleteAsync(1, 5, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_WhenServiceFails_ReturnsError()
    {
        SetupAuthenticatedUser("5");
        _promotionService.DeleteAsync(1, 5, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure(new ServiceError("Promotion introuvable", 404)));

        var result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(404));
    }
}
