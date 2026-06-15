using DeliverTableServer.Common;
using DeliverTableServer.Features.DiscountCode;
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

    [Test]
    public async Task Create_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "5", nameof(UserRole.RestaurantOwner));
        CreateDiscountCodeRequest request = new CreateDiscountCodeRequest();
        DiscountCodeDto dto = new DiscountCodeDto { Id = 1, RestaurantId = 10, Code = "PROMO10" };
        _discountCodeService.CreateAsync(10, 5, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DiscountCodeDto>.Success(dto));

        IActionResult result = await _sut.Create(10, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetByRestaurant_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "5", nameof(UserRole.RestaurantOwner));
        DiscountCodeQuery query = new DiscountCodeQuery();
        PaginatedResult<DiscountCodeDto> paginated = new PaginatedResult<DiscountCodeDto>
        {
            Items = [new DiscountCodeDto { Id = 1, Code = "PROMO10" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };
        _discountCodeService.GetByRestaurantAsync(10, 5, query, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<DiscountCodeDto>>.Success(paginated));

        IActionResult result = await _sut.GetByRestaurant(10, query, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Update_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "5", nameof(UserRole.RestaurantOwner));
        UpdateDiscountCodeRequest request = new UpdateDiscountCodeRequest();
        DiscountCodeDto dto = new DiscountCodeDto { Id = 1, Code = "PROMO20" };
        _discountCodeService.UpdateAsync(1, 5, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DiscountCodeDto>.Success(dto));

        IActionResult result = await _sut.Update(1, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Delete_ReturnsNoContent()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "5", nameof(UserRole.RestaurantOwner));
        _discountCodeService.DeleteAsync(1, 5, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        IActionResult result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }
}
