using DeliverTableServer.Common;
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.DiscountCode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Features.DiscountCode;

[ApiController]
[Authorize]
public class DiscountCodeController(IDiscountCodeService discountCodeService) : ControllerBase
{
    private readonly IDiscountCodeService _discountCodeService = discountCodeService;

    [HttpPost(ApiRoutes.DiscountCode.RestaurantBaseRoute)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    public async Task<IActionResult> Create([FromRoute] int id, [FromBody] CreateDiscountCodeRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        ServiceResult<DiscountCodeDto> result = await _discountCodeService.CreateAsync(id, userId, request, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.DiscountCode.RestaurantBaseRoute)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    public async Task<IActionResult> GetByRestaurant([FromRoute] int id, [FromQuery] DiscountCodeQuery query, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        ServiceResult<PaginatedResult<DiscountCodeDto>> result = await _discountCodeService.GetByRestaurantAsync(id, userId, query, ct);
        return result.ToOkResult();
    }

    [HttpPut(ApiRoutes.DiscountCode.ById)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateDiscountCodeRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        ServiceResult<DiscountCodeDto> result = await _discountCodeService.UpdateAsync(id, userId, request, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.DiscountCode.ValidateRoute)]
    public async Task<IActionResult> Validate([FromRoute] int id, [FromBody] ValidateDiscountCodeRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        ServiceResult<DiscountCodeDto> result = await _discountCodeService.ValidateAsync(id, userId, request.Code, ct: ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.DiscountCode.ById)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        ServiceResult result = await _discountCodeService.DeleteAsync(id, userId, ct);
        return result.ToNoContentResult();
    }

}
