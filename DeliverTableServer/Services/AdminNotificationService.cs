using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Services;

public sealed class AdminNotificationService(
    INotificationRepository notificationRepository,
    IUserRepository userRepository)
    : IAdminNotificationService
{
    private readonly INotificationRepository _notificationRepository = notificationRepository;
    private readonly IUserRepository _userRepository = userRepository;

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
            return ServiceError.NotFound(ErrorMessages.NotificationNotFound);

        return ServiceResult.Success();
    }

    public async Task RaiseForAllAdminsAsync(NotificationType type, string payload, CancellationToken ct = default)
    {
        var admins = await _userRepository.ListByRoleAsync(nameof(UserRole.Administrator), ct);
        if (admins.Count == 0) return;

        var now = DateTime.UtcNow;
        var notifications = admins.Select(admin => new Notification
        {
            UserId = admin.Id,
            Type = type,
            Payload = payload,
            IsRead = false,
            CreatedAt = now,
        });
        await _notificationRepository.CreateManyAsync(notifications, ct);
    }

    public async Task RaiseForUserAsync(int userId, NotificationType type, string payload, CancellationToken ct = default)
    {
        await _notificationRepository.CreateAsync(new Notification
        {
            UserId = userId,
            Type = type,
            Payload = payload,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
        }, ct);
    }
}
