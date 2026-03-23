using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Cart;

namespace DeliverTableServer.Services.Interfaces;

public interface ICartService
{
    Task<ServiceResult<CartDto>> GetCartAsync(int customerId, int restaurantId, CancellationToken ct = default);
    Task<ServiceResult<List<CartDto>>> GetAllCartsAsync(int customerId, CancellationToken ct = default);
    Task<ServiceResult<CartDto>> AddItemAsync(int customerId, AddToCartRequest request, CancellationToken ct = default);
    Task<ServiceResult<CartItemDto>> UpdateItemAsync(int customerId, int cartItemId, UpdateCartItemRequest request, CancellationToken ct = default);
    Task<ServiceResult> RemoveItemAsync(int customerId, int cartItemId, CancellationToken ct = default);
    Task<ServiceResult> ClearCartAsync(int customerId, int restaurantId, CancellationToken ct = default);
}
