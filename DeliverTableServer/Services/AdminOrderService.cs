using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;
using DeliverTableInfrastructure.Models;

namespace DeliverTableServer.Services;

public sealed class AdminOrderService(IOrderRepository orderRepository) : IAdminOrderService
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private static readonly string OrderStatusNames = string.Join(", ", Enum.GetNames<OrderStatus>());

    public async Task<ServiceResult<List<AdminOrderResponse>>> GetAllAsync(CancellationToken ct = default)
    {
        List<Order> orders = await _orderRepository.GetAllUnscopedAsync(ct);
        List<AdminOrderResponse> result = orders.Select(o => o.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminOrderResponse>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        Order? order = await _orderRepository.GetByIdWithFullDetailsAsync(id, ct);
        if (order is null)
            return ServiceError.NotFound(ErrorMessages.OrderNotFound);

        return order.ToAdminDto();
    }

    public async Task<ServiceResult<AdminOrderResponse>> UpdateStatusAsync(
        int id, AdminUpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, out OrderStatus newStatus))
            return ServiceError.BadRequest(ErrorMessages.InvalidOrderStatus(OrderStatusNames));

        Order? order = await _orderRepository.GetByIdWithFullDetailsAsync(id, ct);
        if (order is null)
            return ServiceError.NotFound(ErrorMessages.OrderNotFound);

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        Order updated = await _orderRepository.UpdateAsync(order, ct);
        return updated.ToAdminDto();
    }
}
