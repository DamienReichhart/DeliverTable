using DeliverTableInfrastructure.Models;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByCustomerAndRestaurantAsync(int customerId, int restaurantId, CancellationToken ct = default);
    Task<Cart?> GetByIdWithItemsAsync(int cartId, CancellationToken ct = default);
    Task<List<Cart>> GetByCustomerAsync(int customerId, CancellationToken ct = default);
    Task<Cart> CreateAsync(Cart cart, CancellationToken ct = default);
    Task<Cart> UpdateAsync(Cart cart, CancellationToken ct = default);
    Task<bool> DeleteAsync(int cartId, CancellationToken ct = default);
    Task<CartItem?> GetCartItemByIdAsync(int cartItemId, CancellationToken ct = default);
    Task<CartItem> AddItemAsync(CartItem item, CancellationToken ct = default);
    Task<CartItem> UpdateItemAsync(CartItem item, CancellationToken ct = default);
    Task<bool> RemoveItemAsync(int cartItemId, CancellationToken ct = default);
}
