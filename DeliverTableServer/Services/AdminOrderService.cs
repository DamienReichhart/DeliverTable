using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Services;

public sealed class AdminOrderService(IOrderRepository orderRepository) : IAdminOrderService
{
    private readonly IOrderRepository _orderRepository = orderRepository;

    public async Task<ServiceResult<List<AdminOrderResponse>>> GetAllAsync(CancellationToken ct = default)
    {
        var orders = await _orderRepository.GetAllUnscopedAsync(ct);
        var result = orders.Select(o => o.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminOrderResponse>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetByIdWithFullDetailsAsync(id, ct);
        if (order is null)
            return ServiceError.NotFound(ErrorMessages.OrderNotFound);

        return order.ToAdminDto();
    }

    public async Task<ServiceResult<AdminOrderResponse>> UpdateStatusAsync(
        int id, AdminUpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, out var newStatus))
        {
            var validValues = string.Join(", ", Enum.GetNames<OrderStatus>());
            return ServiceError.BadRequest(ErrorMessages.InvalidOrderStatus(validValues));
        }

        var order = await _orderRepository.GetByIdWithFullDetailsAsync(id, ct);
        if (order is null)
            return ServiceError.NotFound(ErrorMessages.OrderNotFound);

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        var updated = await _orderRepository.UpdateAsync(order, ct);
        return updated.ToAdminDto();
    }
}
