using DeliverTableServer.Extensions;
using DeliverTableServer.Middleware.ActionFilters;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Dish;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Dish.Base)]
public class DishController(IDishService dishService) : ControllerBase
{
    private readonly IDishService _dishService = dishService;

    [HttpGet]
    public async Task<IActionResult> GetAllDishes([FromQuery] DishQuery query, CancellationToken ct)
    {
        var result = await _dishService.GetAllAsync(query, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Dish.ByIdRoute)]
    public async Task<IActionResult> GetDishById(int id, CancellationToken ct)
    {
        var result = await _dishService.GetByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Dish.DishesByRestaurantIdRoute)]
    public async Task<IActionResult> GetDishesByRestaurantId([FromQuery] DishQuery query, int id, CancellationToken ct)
    {
        var result = await _dishService.GetByRestaurantIdAsync(query, id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Dish.DishesByRestaurantIdRoute)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    [EnsureOwner]
    public async Task<IActionResult> CreateDish([FromForm] CreateDishDto createDishDto, [FromRoute] int id, IFormFile? image, CancellationToken ct)
    {
        var result = await _dishService.CreateAsync(createDishDto, id, image, ct);
        return result.ToOkResult();
    }

    [HttpPut(ApiRoutes.Dish.ByIdRoute)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    [RestaurantOwner]
    public async Task<IActionResult> UpdateDish([FromRoute] int id, [FromForm] CreateDishDto createDishDto, IFormFile? image, CancellationToken ct)
    {
        var result = await _dishService.UpdateAsync(id, createDishDto, image, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Dish.ByIdRoute)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    [RestaurantOwner]
    public async Task<IActionResult> DeleteDish([FromRoute] int id, CancellationToken ct)
    {
        var result = await _dishService.DeleteAsync(id, ct);
        return result.ToNoContentResult();
    }
}
