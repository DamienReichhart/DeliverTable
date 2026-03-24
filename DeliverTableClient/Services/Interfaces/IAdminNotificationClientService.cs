using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services.Interfaces;

public interface IAdminNotificationClientService
{
    Task<(List<AdminNotificationResponse>? Notifications, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default);

    Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int id, CancellationToken ct = default);
}
