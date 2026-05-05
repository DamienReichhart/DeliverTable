using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default);
    Task<(List<Order> Items, int TotalCount)> GetByCustomerAsync(int customerId, OrderQuery query, CancellationToken ct = default);
    Task<(List<Order> Items, int TotalCount)> GetByRestaurantAsync(int restaurantId, OrderQuery query, CancellationToken ct = default);
    Task<Order> UpdateAsync(Order order, CancellationToken ct = default);
    Task<List<Order>> GetAllUnscopedAsync(CancellationToken ct = default);
    Task<Order?> GetByIdWithFullDetailsAsync(int id, CancellationToken ct = default);
    Task<List<Order>> GetOrdersOlderThanAsync(OrderStatus status, DateTime threshold, CancellationToken ct = default);
    Task<int> CountScheduledDineInOverlappingAsync(
        int restaurantId,
        DateTime startsAt,
        DateTime endsAt,
        CancellationToken ct = default);
    Task<int> GetScheduledDineInReservedTableUnitsOverlappingAsync(
        int restaurantId,
        DateTime startsAt,
        DateTime endsAt,
        CancellationToken ct = default);
}
