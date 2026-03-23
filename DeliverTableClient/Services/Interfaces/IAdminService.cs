using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services.Interfaces;

public interface IAdminService
{
    /// <summary>Fetches all users from <c>GET /api/v1/admin/users</c>.</summary>
    Task<(List<AdminUserResponse>? Users, ErrorResponse? Error)> GetAllUsersAsync(CancellationToken ct = default);

    /// <summary>Fetches a single user by id from <c>GET /api/v1/admin/users/{id}</c>.</summary>
    Task<(AdminUserResponse? User, ErrorResponse? Error)> GetUserByIdAsync(int userId, CancellationToken ct = default);

    /// <summary>Creates a new user via <c>POST /api/v1/admin/users</c>.</summary>
    Task<(AdminUserResponse? User, ErrorResponse? Error)> CreateUserAsync(
        AdminCreateUserRequest request, CancellationToken ct = default);

    /// <summary>Updates a user's info, role and status via <c>PUT /api/v1/admin/users/{id}</c>.</summary>
    Task<(AdminUserResponse? User, ErrorResponse? Error)> UpdateUserAsync(
        int userId, AdminUpdateUserRequest request, CancellationToken ct = default);

    /// <summary>Deletes a user via <c>DELETE /api/v1/admin/users/{id}</c>.</summary>
    Task<(bool Success, ErrorResponse? Error)> DeleteUserAsync(int userId, CancellationToken ct = default);

    /// <summary>Updates a user's role via <c>PUT /api/v1/admin/users/{id}/role</c>.</summary>
    Task<(AdminUserResponse? User, ErrorResponse? Error)> UpdateUserRoleAsync(
        int userId, UpdateUserRoleRequest request, CancellationToken ct = default);

    /// <summary>Updates a user's status via <c>PUT /api/v1/admin/users/{id}/status</c>.</summary>
    Task<(AdminUserResponse? User, ErrorResponse? Error)> UpdateUserStatusAsync(
        int userId, UpdateUserStatusRequest request, CancellationToken ct = default);
}
