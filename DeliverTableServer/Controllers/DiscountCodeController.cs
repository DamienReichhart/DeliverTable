using System.Security.Claims;
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.DiscountCode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.RestaurantOwner))]
public class DiscountCodeController(IDiscountCodeService discountCodeService) : ControllerBase
{
    private readonly IDiscountCodeService _discountCodeService = discountCodeService;

    [HttpPost(ApiRoutes.DiscountCodeRoutes.RestaurantBaseRoute)]
    public async Task<IActionResult> Create([FromRoute] int id, [FromBody] CreateDiscountCodeRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out int userId)) return Unauthorized();
        var result = await _discountCodeService.CreateAsync(id, userId, request, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.DiscountCodeRoutes.RestaurantBaseRoute)]
    public async Task<IActionResult> GetByRestaurant([FromRoute] int id, [FromQuery] DiscountCodeQuery query, CancellationToken ct)
    {
        if (!TryGetUserId(out int userId)) return Unauthorized();
        var result = await _discountCodeService.GetByRestaurantAsync(id, userId, query, ct);
        return result.ToOkResult();
    }

    [HttpPut(ApiRoutes.DiscountCodeRoutes.Base + "/" + ApiRoutes.DiscountCodeRoutes.ByIdRoute)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateDiscountCodeRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out int userId)) return Unauthorized();
        var result = await _discountCodeService.UpdateAsync(id, userId, request, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.DiscountCodeRoutes.ValidateRoute)]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> Validate([FromRoute] int id, [FromBody] ValidateDiscountCodeRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out int userId)) return Unauthorized();
        var result = await _discountCodeService.ValidateAsync(id, userId, request.Code, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.DiscountCodeRoutes.Base + "/" + ApiRoutes.DiscountCodeRoutes.ByIdRoute)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        if (!TryGetUserId(out int userId)) return Unauthorized();
        var result = await _discountCodeService.DeleteAsync(id, userId, ct);
        return result.ToNoContentResult();
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
    }
}
