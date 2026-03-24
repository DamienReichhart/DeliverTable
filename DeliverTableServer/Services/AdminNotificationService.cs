using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services;

public sealed class AdminNotificationService(INotificationRepository notificationRepository)
    : IAdminNotificationService
{
    private readonly INotificationRepository _notificationRepository = notificationRepository;

    public async Task<ServiceResult<List<AdminNotificationResponse>>> GetAllAsync(CancellationToken ct = default)
    {
        var notifications = await _notificationRepository.GetAllAsync(ct);
        var result = notifications.Select(n => n.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var deleted = await _notificationRepository.DeleteAsync(id, ct);
        if (!deleted)
            return new ServiceError(ErrorMessages.NotificationNotFound, 404);

        return ServiceResult.Success();
    }
}
