using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Order;

namespace DeliverTableServer.Services.Interfaces;

public interface IOrderService
{
    Task<ServiceResult<OrderDto>> CreateFromCartAsync(int customerId, CreateOrderRequest request, CancellationToken ct = default);
    Task<ServiceResult<OrderDto>> GetByIdAsync(int orderId, int userId, CancellationToken ct = default);
    Task<ServiceResult<PaginatedResult<OrderDto>>> GetCustomerOrdersAsync(int customerId, OrderQuery query, CancellationToken ct = default);
    Task<ServiceResult<PaginatedResult<OrderDto>>> GetRestaurantOrdersAsync(int restaurantId, int ownerId, OrderQuery query, CancellationToken ct = default);
    Task<ServiceResult<OrderDto>> UpdateStatusAsync(int orderId, UpdateOrderStatusRequest request, CancellationToken ct = default);
    Task<ServiceResult<OrderDto>> CancelOrderAsync(int orderId, int customerId, CancellationToken ct = default);
}
