using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Mappers;

public static class AdminModerationMapper
{
    public static AdminModerationActionResponse ToAdminDto(this ModerationAction action)
    {
        return new AdminModerationActionResponse
        {
            Id = action.Id,
            TargetType = action.TargetType.ToString(),
            TargetId = action.TargetId,
            ActionType = action.ActionType.ToString(),
            Reason = action.Reason,
            AdminUserName = action.AdminUser is not null
                ? $"{action.AdminUser.FirstName} {action.AdminUser.LastName}"
                : "",
            AdminUserId = action.AdminUserId,
            CreatedAt = action.CreatedAt
        };
    }
}
