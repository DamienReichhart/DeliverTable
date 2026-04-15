using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Services.Interfaces;

public interface IAdminNotificationService
{
    Task<ServiceResult<List<AdminNotificationResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default);
    Task RaiseForAllAdminsAsync(NotificationType type, string payload, CancellationToken ct = default);
    Task RaiseForUserAsync(int userId, NotificationType type, string payload, CancellationToken ct = default);
}
