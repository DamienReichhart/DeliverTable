using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services.Interfaces;

public interface IAdminService
{
    Task<ServiceResult<List<AdminUserResponse>>> GetAllUsersAsync(CancellationToken ct = default);
    Task<ServiceResult<AdminUserResponse>> GetUserByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<AdminUserResponse>> CreateUserAsync(AdminCreateUserRequest request, CancellationToken ct = default);
    Task<ServiceResult<AdminUserResponse>> UpdateUserAsync(int id, AdminUpdateUserRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteUserAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<AdminUserResponse>> UpdateUserRoleAsync(int id, UpdateUserRoleRequest request, CancellationToken ct = default);
    Task<ServiceResult<AdminUserResponse>> UpdateUserStatusAsync(int id, UpdateUserStatusRequest request, CancellationToken ct = default);
}
