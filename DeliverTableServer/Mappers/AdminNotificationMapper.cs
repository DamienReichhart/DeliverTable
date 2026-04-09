using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Mappers;

public static class AdminNotificationMapper
{
    public static AdminNotificationResponse ToAdminDto(this Notification notification)
    {
        return new AdminNotificationResponse
        {
            Id = notification.Id,
            Type = notification.Type.ToString(),
            Payload = notification.Payload,
            IsRead = notification.IsRead,
            UserName = notification.User is not null
                ? $"{notification.User.FirstName} {notification.User.LastName}"
                : "",
            UserId = notification.UserId,
            CreatedAt = notification.CreatedAt
        };
    }
}
