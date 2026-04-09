using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Dtos.Auth;

namespace DeliverTableServer.Mappers;

public static class UserMappers
{
    public static UserResponse ToDto(this User userModel, string? role = null)
    {
        return new UserResponse
        {
            Id = userModel.Id,
            FirstName = userModel.FirstName,
            LastName = userModel.LastName,
            Email = userModel.Email ?? "",
            Role = role ?? "Non défini"
        };
    }

    public static AdminUserResponse ToAdminDto(this User userModel, string? role = null)
    {
        return new AdminUserResponse
        {
            Id = userModel.Id,
            FirstName = userModel.FirstName,
            LastName = userModel.LastName,
            Email = userModel.Email ?? "",
            Role = role ?? "Non défini",
            Status = userModel.Status.ToString(),
            CreatedAt = userModel.CreatedAt,
            UpdatedAt = userModel.UpdatedAt
        };
    }
}
