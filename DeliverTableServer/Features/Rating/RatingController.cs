using DeliverTableServer.Common;
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Rating;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Features.Rating;

[ApiController]
[Route(ApiRoutes.Order.Base)]
[Authorize(Roles = nameof(UserRole.Customer))]
public class RatingController(IRatingService ratingService) : ControllerBase
{
    private readonly IRatingService _ratingService = ratingService;

    [HttpPost(ApiRoutes.Order.RatingRoute)]
    public async Task<IActionResult> Create(
        [FromRoute] int orderId,
        [FromBody] CreateRatingRequest request,
        CancellationToken ct
    )
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        ServiceResult<RatingDto> result = await _ratingService.CreateAsync(orderId, userId, request, ct);
        return result.ToCreatedResult(nameof(GetByOrder), _ => new { orderId });
    }

    [HttpGet(ApiRoutes.Order.RatingRoute)]
    public async Task<IActionResult> GetByOrder([FromRoute] int orderId, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        ServiceResult<RatingDto> result = await _ratingService.GetByOrderAsync(orderId, userId, ct);
        return result.ToOkResult();
    }

    [HttpPut(ApiRoutes.Order.RatingRoute)]
    public async Task<IActionResult> Update(
        [FromRoute] int orderId,
        [FromBody] UpdateRatingRequest request,
        CancellationToken ct
    )
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        ServiceResult<RatingDto> result = await _ratingService.UpdateAsync(orderId, userId, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Order.RatingRoute)]
    public async Task<IActionResult> Delete([FromRoute] int orderId, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        ServiceResult result = await _ratingService.DeleteAsync(orderId, userId, ct);
        return result.ToNoContentResult();
    }
}
