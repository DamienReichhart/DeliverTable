using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Order;

namespace DeliverTableClient.Services.Interfaces;

public interface IOrderService
{
    Task<(OrderDto?, ErrorResponse?)> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<(OrderDto?, ErrorResponse?)> GetOrderByIdAsync(int orderId, CancellationToken ct = default);
    Task<(PaginatedResult<OrderDto>?, ErrorResponse?)> GetMyOrdersAsync(OrderQuery query, CancellationToken ct = default);
    Task<(OrderDto?, ErrorResponse?)> CancelOrderAsync(int orderId, CancellationToken ct = default);
    Task<(PaginatedResult<OrderDto>?, ErrorResponse?)> GetRestaurantOrdersAsync(int restaurantId, OrderQuery query, CancellationToken ct = default);
    Task<(OrderDto?, ErrorResponse?)> UpdateOrderStatusAsync(int orderId, string status, CancellationToken ct = default);
}
