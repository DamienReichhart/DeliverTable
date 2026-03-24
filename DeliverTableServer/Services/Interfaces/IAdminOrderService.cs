using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services.Interfaces;

public interface IAdminOrderService
{
    Task<ServiceResult<List<AdminOrderResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResult<AdminOrderResponse>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<AdminOrderResponse>> UpdateStatusAsync(int id, AdminUpdateOrderStatusRequest request, CancellationToken ct = default);
}
