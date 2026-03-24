using DeliverTableServer.Data;
using DeliverTableServer.Extensions;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories;

public class OrderRepository(DeliverTableContext dbContext) : IOrderRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<Order> CreateAsync(Order order, CancellationToken ct = default)
    {
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(ct);
        return order;
    }

    public async Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default)
    {
        return await _dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.Restaurant)
            .Include(o => o.Discounts)
            .Include(o => o.Payments)
            .Include(o => o.RestaurantTable)
            .Include(o => o.Event)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
    }

    public async Task<(List<Order> Items, int TotalCount)> GetByCustomerAsync(
        int customerId, OrderQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.Restaurant)
            .Include(o => o.Discounts)
            .Include(o => o.Payments)
            .Include(o => o.RestaurantTable)
            .Include(o => o.Event)
            .Where(o => o.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<OrderStatus>(query.Status, out var status))
            q = q.Where(o => o.Status == status);

        q = q.OrderByDescending(o => o.CreatedAt);

        var totalCount = await q.CountAsync(ct);

        var (skip, take) = PaginationExtensions.GetPaginationOffsets(query.PageNumber, query.PageSize);
        var items = await q.Skip(skip).Take(take).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<(List<Order> Items, int TotalCount)> GetByRestaurantAsync(
        int restaurantId, OrderQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.Restaurant)
            .Include(o => o.Discounts)
            .Include(o => o.Payments)
            .Include(o => o.RestaurantTable)
            .Include(o => o.Event)
            .Where(o => o.RestaurantId == restaurantId);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<OrderStatus>(query.Status, out var status))
            q = q.Where(o => o.Status == status);

        q = q.OrderByDescending(o => o.CreatedAt);

        var totalCount = await q.CountAsync(ct);

        var (skip, take) = PaginationExtensions.GetPaginationOffsets(query.PageNumber, query.PageSize);
        var items = await q.Skip(skip).Take(take).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<Order> UpdateAsync(Order order, CancellationToken ct = default)
    {
        order.UpdatedAt = DateTime.UtcNow;
        _dbContext.Orders.Update(order);
        await _dbContext.SaveChangesAsync(ct);
        return order;
    }
}
