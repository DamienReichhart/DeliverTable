using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class CartRepository(DeliverTableContext dbContext) : ICartRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<Cart?> GetByCustomerAndRestaurantAsync(int customerId, int restaurantId, CancellationToken ct = default)
    {
        return await _dbContext.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Dish)
            .Include(c => c.Restaurant)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.RestaurantId == restaurantId, ct);
    }

    public async Task<Cart?> GetByIdWithItemsAsync(int cartId, CancellationToken ct = default)
    {
        return await _dbContext.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Dish)
            .Include(c => c.Restaurant)
            .FirstOrDefaultAsync(c => c.Id == cartId, ct);
    }

    public async Task<List<Cart>> GetByCustomerAsync(int customerId, CancellationToken ct = default)
    {
        return await _dbContext.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Dish)
            .Include(c => c.Restaurant)
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);
    }

    public async Task<Cart> CreateAsync(Cart cart, CancellationToken ct = default)
    {
        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync(ct);
        return cart;
    }

    public async Task<Cart> UpdateAsync(Cart cart, CancellationToken ct = default)
    {
        cart.UpdatedAt = DateTime.UtcNow;
        _dbContext.Carts.Update(cart);
        await _dbContext.SaveChangesAsync(ct);
        return cart;
    }

    public async Task<bool> DeleteAsync(int cartId, CancellationToken ct = default)
    {
        Cart? cart = await _dbContext.Carts.FirstOrDefaultAsync(c => c.Id == cartId, ct);
        if (cart is null) return false;

        _dbContext.Carts.Remove(cart);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<CartItem?> GetCartItemByIdAsync(int cartItemId, CancellationToken ct = default)
    {
        return await _dbContext.CartItems
            .Include(ci => ci.Dish)
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId, ct);
    }

    public async Task<CartItem> AddItemAsync(CartItem item, CancellationToken ct = default)
    {
        _dbContext.CartItems.Add(item);
        await _dbContext.SaveChangesAsync(ct);

        await _dbContext.Entry(item).Reference(i => i.Dish).LoadAsync(ct);
        return item;
    }

    public async Task<CartItem> UpdateItemAsync(CartItem item, CancellationToken ct = default)
    {
        item.UpdatedAt = DateTime.UtcNow;
        _dbContext.CartItems.Update(item);
        await _dbContext.SaveChangesAsync(ct);
        return item;
    }

    public async Task<bool> RemoveItemAsync(int cartItemId, CancellationToken ct = default)
    {
        CartItem? item = await _dbContext.CartItems.FirstOrDefaultAsync(ci => ci.Id == cartItemId, ct);
        if (item is null) return false;

        _dbContext.CartItems.Remove(item);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }
}
