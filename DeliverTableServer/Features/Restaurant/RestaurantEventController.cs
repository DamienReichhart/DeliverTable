using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Event;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Features.Restaurant;

[ApiController]
[Authorize(Roles = nameof(UserRole.RestaurantOwner))]
public class RestaurantEventController(IRestaurantEventService eventService) : ControllerBase
{
    private readonly IRestaurantEventService _eventService = eventService;

    [HttpGet(ApiRoutes.Event.RestaurantBaseRoute)]
    public async Task<IActionResult> GetByRestaurant([FromRoute] int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        var result = await _eventService.GetByRestaurantAsync(id, userId, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Event.ActiveRoute)]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveByRestaurant([FromRoute] int id, CancellationToken ct)
    {
        var result = await _eventService.GetActiveByRestaurantAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Event.RestaurantBaseRoute)]
    public async Task<IActionResult> Create([FromRoute] int id, [FromBody] CreateRestaurantEventRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        var result = await _eventService.CreateAsync(id, userId, request, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Event.ById)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        var result = await _eventService.GetByIdAsync(id, userId, ct);
        return result.ToOkResult();
    }

    [HttpPut(ApiRoutes.Event.ById)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateRestaurantEventRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        var result = await _eventService.UpdateAsync(id, userId, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Event.ById)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        var result = await _eventService.DeleteAsync(id, userId, ct);
        return result.ToNoContentResult();
    }
}
