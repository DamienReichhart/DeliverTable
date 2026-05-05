using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Extensions;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

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
            .IncludeOrderAggregate()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
    }

    public async Task<(List<Order> Items, int TotalCount)> GetByCustomerAsync(
        int customerId, OrderQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.Orders
            .IncludeOrderAggregate()
            .Where(o => o.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<OrderStatus>(query.Status, out var status))
            q = q.Where(o => o.Status == status);

        q = q.OrderByDescending(o => o.CreatedAt);

        var totalCount = await q.CountAsync(ct);
        var items = await q.Paginate(query.PageNumber, query.PageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<(List<Order> Items, int TotalCount)> GetByRestaurantAsync(
        int restaurantId, OrderQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.Orders
            .IncludeOrderAggregate()
            .Where(o => o.RestaurantId == restaurantId);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<OrderStatus>(query.Status, out var status))
            q = q.Where(o => o.Status == status);

        if (query.CreatedAfter.HasValue)
            q = q.Where(o => o.CreatedAt >= query.CreatedAfter.Value);

        if (query.ToPrepare is true)
            q = q.Where(o => o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Preparing);

        q = ApplySorting(q, query);

        var totalCount = await q.CountAsync(ct);
        var items = await q.Paginate(query.PageNumber, query.PageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<Order> UpdateAsync(Order order, CancellationToken ct = default)
    {
        order.UpdatedAt = DateTime.UtcNow;
        _dbContext.Orders.Update(order);
        await _dbContext.SaveChangesAsync(ct);
        return order;
    }

    public async Task<List<Order>> GetAllUnscopedAsync(CancellationToken ct = default)
    {
        return await _dbContext.Orders
            .Include(o => o.Customer)
            .Include(o => o.Restaurant)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Order?> GetByIdWithFullDetailsAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Orders
            .Include(o => o.Customer)
            .Include(o => o.Restaurant)
            .Include(o => o.Items)
                .ThenInclude(i => i.Dish)
            .Include(o => o.Payments)
            .Include(o => o.Discounts)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public Task<List<Order>> GetOrdersOlderThanAsync(OrderStatus status, DateTime threshold, CancellationToken ct = default) =>
        _dbContext.Orders
            .Where(o => o.Status == status && o.CreatedAt < threshold)
            .ToListAsync(ct);

    private IQueryable<Order> ApplySorting(IQueryable<Order> query, OrderQuery request)
    {
        var property = request.SortBy switch
        {
            "CreatedAt" => nameof(Order.CreatedAt),
            "Status" => nameof(Order.Status),
            "Id" => nameof(Order.Id),
            "OrderType" => nameof(Order.OrderType),
            "PaymentStatus" => nameof(Order.PaymentStatus),
            "TotalAmount" => nameof(Order.TotalAmount),
            "OriginalAmount" => nameof(Order.OriginalAmount),
            "DiscountAmount" => nameof(Order.DiscountAmount),
            "LoyaltyPointsUsed" => nameof(Order.LoyaltyPointsUsed),
            "LoyaltyPointsEarned" => nameof(Order.LoyaltyPointsEarned),
            "GuestCount" => nameof(Order.GuestCount),
            "DeliveryAddress" => nameof(Order.DeliveryAddress),
            "Notes" => nameof(Order.Notes),
            "ScheduledAt" => nameof(Order.ScheduledAt),
            "RestaurantTableId" => nameof(Order.RestaurantTableId),
            "IsEventBooking" => nameof(Order.IsEventBooking),
            "EventId" => nameof(Order.EventId),
            _ => nameof(Order.CreatedAt)
        };

        if (request.SortDesc is true)
        {
            query = query.OrderByDescending(o => o.CreatedAt);
        }
        else
        {
            query = query.OrderBy(o => o.CreatedAt);
        }

        return query;
    }
}
