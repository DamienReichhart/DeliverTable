using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Cart;

namespace DeliverTableClient.Services.Interfaces;

public interface ICartService
{
    Task<(CartDto?, ErrorResponse?)> GetCartAsync(int restaurantId, CancellationToken ct = default);
    Task<(List<CartDto>?, ErrorResponse?)> GetAllCartsAsync(CancellationToken ct = default);
    Task<(CartDto?, ErrorResponse?)> AddItemAsync(AddToCartRequest request, CancellationToken ct = default);
    Task<(CartItemDto?, ErrorResponse?)> UpdateItemAsync(int cartItemId, UpdateCartItemRequest request, CancellationToken ct = default);
    Task<ErrorResponse?> RemoveItemAsync(int cartItemId, CancellationToken ct = default);
    Task<ErrorResponse?> ClearCartAsync(int restaurantId, CancellationToken ct = default);
}
