using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.RestaurantOwner))]
public class RestaurantOrderConfigController(IRestaurantOrderConfigService restaurantOrderConfigService) : ControllerBase
{
    private readonly IRestaurantOrderConfigService _restaurantOrderConfigService = restaurantOrderConfigService;

    [HttpGet(ApiRoutes.OrderConfig.RestaurantBlockedSlotsRoute)]
    public async Task<IActionResult> GetBlockedSlots([FromRoute] int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId))
            return Unauthorized();

        var result = await _restaurantOrderConfigService.GetBlockedSlotsAsync(id, userId, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.OrderConfig.RestaurantBlockedSlotsRoute)]
    public async Task<IActionResult> CreateBlockedSlot(
        [FromRoute] int id,
        [FromBody] AdminCreateBlockedSlotRequest request,
        CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId))
            return Unauthorized();

        var result = await _restaurantOrderConfigService.CreateBlockedSlotAsync(id, userId, request, ct);
        if (!result.IsSuccess)
            return result.Error!.ToErrorResult();

        return Ok(result.Value);
    }

    [HttpDelete(ApiRoutes.OrderConfig.RestaurantBlockedSlotByIdRoute)]
    public async Task<IActionResult> DeleteBlockedSlot([FromRoute] int id, [FromRoute] int slotId, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId))
            return Unauthorized();

        var result = await _restaurantOrderConfigService.DeleteBlockedSlotAsync(id, slotId, userId, ct);
        return result.ToNoContentResult();
    }

    [AllowAnonymous]
    [HttpGet(ApiRoutes.OrderConfig.TablesCapacityRoute)]
    public async Task<IActionResult> GetTablesCapacity([FromRoute] int id, CancellationToken ct)
    {
        var result = await _restaurantOrderConfigService.GetTablesCapacityAsync(id, 0, ct);
        return result.ToOkResult();
    }

    [HttpPut(ApiRoutes.OrderConfig.TablesCapacityRoute)]
    public async Task<IActionResult> UpdateTablesCapacity(
        [FromRoute] int id,
        [FromBody] UpdateTablesCapacityRequest request,
        CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId))
            return Unauthorized();

        var result = await _restaurantOrderConfigService.UpdateTablesCapacityAsync(id, userId, request, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.OrderConfig.OpeningHoursRoute)]
    public async Task<IActionResult> GetOpeningHours([FromRoute] int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId))
            return Unauthorized();

        var result = await _restaurantOrderConfigService.GetOpeningHoursAsync(id, userId, ct);
        return result.ToOkResult();
    }

    [HttpPut(ApiRoutes.OrderConfig.OpeningHoursRoute)]
    public async Task<IActionResult> UpdateOpeningHours(
        [FromRoute] int id,
        [FromBody] UpdateRestaurantOpeningHoursRequest request,
        CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId))
            return Unauthorized();

        var result = await _restaurantOrderConfigService.UpdateOpeningHoursAsync(id, userId, request, ct);
        return result.ToOkResult();
    }

    [AllowAnonymous]
    [HttpGet(ApiRoutes.OrderConfig.AvailableSlotsRoute)]
    public async Task<IActionResult> GetAvailableSlots(
        [FromRoute] int id,
        [FromQuery] RestaurantAvailableSlotsQuery query,
        CancellationToken ct)
    {
        var result = await _restaurantOrderConfigService.GetAvailableSlotsAsync(id, query, ct);
        return result.ToOkResult();
    }
}
