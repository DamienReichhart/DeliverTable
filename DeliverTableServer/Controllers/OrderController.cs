using System.Security.Claims;
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Order.Base)]
[Authorize]
public class OrderController(IOrderService orderService) : ControllerBase
{
    private readonly IOrderService _orderService = orderService;

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        var result = await _orderService.CreateFromCartAsync(userId, request, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);

        return result.Error!.ToErrorResult();
    }

    [HttpGet(ApiRoutes.Order.ByIdRoute)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        var result = await _orderService.GetByIdAsync(id, userId, ct);
        return result.ToOkResult();
    }

    [HttpGet]
    public async Task<IActionResult> GetMyOrders([FromQuery] OrderQuery query, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        var result = await _orderService.GetCustomerOrdersAsync(userId, query, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Order.RestaurantOrdersRoute)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    public async Task<IActionResult> GetRestaurantOrders([FromRoute] int id, [FromQuery] OrderQuery query, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        var result = await _orderService.GetRestaurantOrdersAsync(id, userId, query, ct);
        return result.ToOkResult();
    }

    [HttpPut(ApiRoutes.Order.StatusRoute)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    public async Task<IActionResult> UpdateStatus([FromRoute] int id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        var result = await _orderService.UpdateStatusAsync(id, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Order.ByIdRoute)]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> CancelOrder([FromRoute] int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        var result = await _orderService.CancelOrderAsync(id, userId, ct);
        return result.ToOkResult();
    }

}
