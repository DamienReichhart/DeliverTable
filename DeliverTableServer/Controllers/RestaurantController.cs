using DeliverTableServer.Extensions;
using DeliverTableServer.Middleware.ActionFilters;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Restaurant.Base)]
public class RestaurantController(IRestaurantService restaurantService) : ControllerBase
{
    private readonly IRestaurantService _restaurantService = restaurantService;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] RestaurantQuery query, CancellationToken ct)
    {
        var result = await _restaurantService.GetAllAsync(query, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Restaurant.MapRoute)]
    public async Task<IActionResult> GetForMap([FromQuery] RestaurantQuery query, CancellationToken ct)
    {
        var result = await _restaurantService.GetForMapAsync(query, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Restaurant.UserByIdRoute)]
    [HttpGet(ApiRoutes.Restaurant.UserMeRoute)]
    [Authorize]
    public async Task<IActionResult> GetAllUserRestaurants(
        [FromQuery] RestaurantQuery query, CancellationToken ct, [FromRoute] int? id = null)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        if (id is not null)
        {
            if (!User.IsInRole(nameof(UserRole.Administrator)) && userId != id)
                return Forbid();
        }
        else
        {
            id = userId;
        }

        var result = await _restaurantService.GetByOwnerAsync(id.Value, query, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Restaurant.ByIdRoute)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var result = await _restaurantService.GetByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPut(ApiRoutes.Restaurant.ByIdRoute)]
    [EnsureOwner]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateRestaurantDto restaurantDto, CancellationToken ct)
    {
        var result = await _restaurantService.UpdateAsync(id, restaurantDto, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Restaurant.ByIdRoute)]
    [EnsureOwner]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var result = await _restaurantService.DeleteAsync(id, ct);
        return result.ToNoContentResult();
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    public async Task<IActionResult> Create([FromBody] CreateRestaurantDto creationDto, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int ownerId)) return Unauthorized();

        var result = await _restaurantService.CreateAsync(creationDto, ownerId, ct);
        return result.ToCreatedResult(nameof(GetById), v => new { id = v.Id });
    }
}
