using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Cart.Base)]
[Authorize(Roles = nameof(UserRole.Customer))]
public class CartController(ICartService cartService) : ControllerBase
{
    private readonly ICartService _cartService = cartService;

    [HttpGet(ApiRoutes.Cart.ByRestaurantRoute)]
    public async Task<IActionResult> GetCart([FromRoute] int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        var result = await _cartService.GetCartAsync(userId, id, ct);
        return result.ToOkResult();
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCarts(CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        var result = await _cartService.GetAllCartsAsync(userId, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Cart.ItemsRoute)]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        var result = await _cartService.AddItemAsync(userId, request, ct);
        return result.ToOkResult();
    }

    [HttpPut(ApiRoutes.Cart.ItemByIdRoute)]
    public async Task<IActionResult> UpdateItem([FromRoute] int id, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        var result = await _cartService.UpdateItemAsync(userId, id, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Cart.ItemByIdRoute)]
    public async Task<IActionResult> RemoveItem([FromRoute] int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        var result = await _cartService.RemoveItemAsync(userId, id, ct);
        return result.ToNoContentResult();
    }

    [HttpDelete(ApiRoutes.Cart.ByRestaurantRoute)]
    public async Task<IActionResult> ClearCart([FromRoute] int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        var result = await _cartService.ClearCartAsync(userId, id, ct);
        return result.ToNoContentResult();
    }

}
