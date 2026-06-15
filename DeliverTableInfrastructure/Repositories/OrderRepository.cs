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
        IQueryable<Order> q = _dbContext.Orders
            .IncludeOrderAggregate()
            .Where(o => o.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<OrderStatus>(query.Status, out OrderStatus status))
            q = q.Where(o => o.Status == status);

        q = q.OrderByDescending(o => o.CreatedAt);

        int totalCount = await q.CountAsync(ct);
        List<Order> items = await q.Paginate(query.PageNumber, query.PageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<(List<Order> Items, int TotalCount)> GetByRestaurantAsync(
        int restaurantId, OrderQuery query, CancellationToken ct = default)
    {
        IQueryable<Order> q = _dbContext.Orders
            .IncludeOrderAggregate()
            .Where(o => o.RestaurantId == restaurantId);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<OrderStatus>(query.Status, out OrderStatus status))
            q = q.Where(o => o.Status == status);

        if (query.CreatedAfter.HasValue)
            q = q.Where(o => o.CreatedAt >= query.CreatedAfter.Value);

        if (query.ToPrepare is true)
            q = q.Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Preparing);
        q = ApplySorting(q, query);

        int totalCount = await q.CountAsync(ct);
        List<Order> items = await q.Paginate(query.PageNumber, query.PageSize).ToListAsync(ct);
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

    public async Task<int> CountScheduledDineInOverlappingAsync(
        int restaurantId,
        DateTime startsAt,
        DateTime endsAt,
        CancellationToken ct = default)
    {
        return await _dbContext.Orders.CountAsync(o =>
            o.RestaurantId == restaurantId
            && o.OrderType == OrderType.DineIn
            && o.GuestCount <= 2
            && o.ScheduledAt.HasValue
            && o.ScheduledAt.Value >= startsAt
            && o.ScheduledAt.Value < endsAt
            && o.Status != OrderStatus.Cancelled
            && o.Status != OrderStatus.Refused,
            ct);
    }

    public async Task<int> GetScheduledDineInReservedTableUnitsOverlappingAsync(
        int restaurantId,
        DateTime startsAt,
        DateTime endsAt,
        CancellationToken ct = default)
    {
        return await _dbContext.Orders
            .Where(o =>
                o.RestaurantId == restaurantId
                && o.OrderType == OrderType.DineIn
                && o.ScheduledAt.HasValue
                && o.ScheduledAt.Value >= startsAt
                && o.ScheduledAt.Value < endsAt
                && o.Status != OrderStatus.Cancelled
                && o.Status != OrderStatus.Refused)
            .SumAsync(o => (o.GuestCount + 1) / 2, ct);
    }

    private IQueryable<Order> ApplySorting(IQueryable<Order> query, OrderQuery request)
    {
        string property = request.SortBy switch
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

        query = (property, request.SortDesc) switch
        {
            (nameof(Order.Status), true) => query.OrderByDescending(o => o.Status),
            (nameof(Order.Status), false) => query.OrderBy(o => o.Status),
            (nameof(Order.Id), true) => query.OrderByDescending(o => o.Id),
            (nameof(Order.Id), false) => query.OrderBy(o => o.Id),
            (nameof(Order.OrderType), true) => query.OrderByDescending(o => o.OrderType),
            (nameof(Order.OrderType), false) => query.OrderBy(o => o.OrderType),
            (nameof(Order.PaymentStatus), true) => query.OrderByDescending(o => o.PaymentStatus),
            (nameof(Order.PaymentStatus), false) => query.OrderBy(o => o.PaymentStatus),
            (nameof(Order.TotalAmount), true) => query.OrderByDescending(o => o.TotalAmount),
            (nameof(Order.TotalAmount), false) => query.OrderBy(o => o.TotalAmount),
            (nameof(Order.OriginalAmount), true) => query.OrderByDescending(o => o.OriginalAmount),
            (nameof(Order.OriginalAmount), false) => query.OrderBy(o => o.OriginalAmount),
            (nameof(Order.DiscountAmount), true) => query.OrderByDescending(o => o.DiscountAmount),
            (nameof(Order.DiscountAmount), false) => query.OrderBy(o => o.DiscountAmount),
            (nameof(Order.LoyaltyPointsUsed), true) => query.OrderByDescending(o => o.LoyaltyPointsUsed),
            (nameof(Order.LoyaltyPointsUsed), false) => query.OrderBy(o => o.LoyaltyPointsUsed),
            (nameof(Order.LoyaltyPointsEarned), true) => query.OrderByDescending(o => o.LoyaltyPointsEarned),
            (nameof(Order.LoyaltyPointsEarned), false) => query.OrderBy(o => o.LoyaltyPointsEarned),
            (nameof(Order.GuestCount), true) => query.OrderByDescending(o => o.GuestCount),
            (nameof(Order.GuestCount), false) => query.OrderBy(o => o.GuestCount),
            (nameof(Order.DeliveryAddress), true) => query.OrderByDescending(o => o.DeliveryAddress),
            (nameof(Order.DeliveryAddress), false) => query.OrderBy(o => o.DeliveryAddress),
            (nameof(Order.Notes), true) => query.OrderByDescending(o => o.Notes),
            (nameof(Order.Notes), false) => query.OrderBy(o => o.Notes),
            (nameof(Order.ScheduledAt), true) => query.OrderByDescending(o => o.ScheduledAt),
            (nameof(Order.ScheduledAt), false) => query.OrderBy(o => o.ScheduledAt),
            (nameof(Order.RestaurantTableId), true) => query.OrderByDescending(o => o.RestaurantTableId),
            (nameof(Order.RestaurantTableId), false) => query.OrderBy(o => o.RestaurantTableId),
            (nameof(Order.IsEventBooking), true) => query.OrderByDescending(o => o.IsEventBooking),
            (nameof(Order.IsEventBooking), false) => query.OrderBy(o => o.IsEventBooking),
            (nameof(Order.EventId), true) => query.OrderByDescending(o => o.EventId),
            (nameof(Order.EventId), false) => query.OrderBy(o => o.EventId),
            (_, true) => query.OrderByDescending(o => o.CreatedAt),
            _ => query.OrderBy(o => o.CreatedAt)
        };

        return query;
    }
}
