using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.DiscountCode;
using DeliverTableTests.Global.Helpers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class DiscountCodeControllerTests
{
    private IDiscountCodeService _discountCodeService = null!;
    private DiscountCodeController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _discountCodeService = Substitute.For<IDiscountCodeService>();
        _sut = new DiscountCodeController(_discountCodeService);
    }

    private void SetupAuthenticatedUser(string userId, string role = nameof(UserRole.RestaurantOwner))
        => AuthenticationTestHelper.SetupAuthenticatedUser(_sut, userId, role);

    [Test]
    public async Task Create_ReturnsOk()
    {
        SetupAuthenticatedUser("5");
        var request = new CreateDiscountCodeRequest();
        var dto = new DiscountCodeDto { Id = 1, RestaurantId = 10, Code = "PROMO10" };
        _discountCodeService.CreateAsync(10, 5, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DiscountCodeDto>.Success(dto));

        var result = await _sut.Create(10, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetByRestaurant_ReturnsOk()
    {
        SetupAuthenticatedUser("5");
        var query = new DiscountCodeQuery();
        var paginated = new PaginatedResult<DiscountCodeDto>
        {
            Items = [new DiscountCodeDto { Id = 1, Code = "PROMO10" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };
        _discountCodeService.GetByRestaurantAsync(10, 5, query, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<DiscountCodeDto>>.Success(paginated));

        var result = await _sut.GetByRestaurant(10, query, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Update_ReturnsOk()
    {
        SetupAuthenticatedUser("5");
        var request = new UpdateDiscountCodeRequest();
        var dto = new DiscountCodeDto { Id = 1, Code = "PROMO20" };
        _discountCodeService.UpdateAsync(1, 5, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DiscountCodeDto>.Success(dto));

        var result = await _sut.Update(1, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Delete_ReturnsNoContent()
    {
        SetupAuthenticatedUser("5");
        _discountCodeService.DeleteAsync(1, 5, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }
}
