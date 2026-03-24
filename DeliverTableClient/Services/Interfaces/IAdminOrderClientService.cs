using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services.Interfaces;

public interface IAdminOrderClientService
{
    Task<(List<AdminOrderResponse>? Orders, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default);

    Task<(AdminOrderResponse? Order, ErrorResponse? Error)> GetByIdAsync(
        int id, CancellationToken ct = default);

    Task<(AdminOrderResponse? Order, ErrorResponse? Error)> UpdateStatusAsync(
        int id, AdminUpdateOrderStatusRequest request, CancellationToken ct = default);
}
