using DeliverTableServer.Common;
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Promotion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Features.Promotion;

[ApiController]
[Authorize(Roles = nameof(UserRole.RestaurantOwner))]
public class PromotionController(IPromotionService promotionService) : ControllerBase
{
    private readonly IPromotionService _promotionService = promotionService;

    [HttpPost(ApiRoutes.Promotion.RestaurantBaseRoute)]
    public async Task<IActionResult> Create([FromRoute] int id, [FromBody] CreatePromotionRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        ServiceResult<PromotionDto> result = await _promotionService.CreateAsync(id, userId, request, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Promotion.RestaurantBaseRoute)]
    public async Task<IActionResult> GetByRestaurant([FromRoute] int id, [FromQuery] PromotionQuery query, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        ServiceResult<PaginatedResult<PromotionDto>> result = await _promotionService.GetByRestaurantAsync(id, userId, query, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Promotion.ActiveRoute)]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveByRestaurant([FromRoute] int id, CancellationToken ct)
    {
        ServiceResult<List<PromotionDto>> result = await _promotionService.GetActiveByRestaurantAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPut(ApiRoutes.Promotion.ById)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdatePromotionRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        ServiceResult<PromotionDto> result = await _promotionService.UpdateAsync(id, userId, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Promotion.ById)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        ServiceResult result = await _promotionService.DeleteAsync(id, userId, ct);
        return result.ToNoContentResult();
    }

}
