using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Enums;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services;

public sealed class AdminService(IUserRepository userRepository) : IAdminService
{
    private static readonly string[] ValidRoles = Enum.GetNames<UserRole>();
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<ServiceResult<List<AdminUserResponse>>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await _userRepository.GetAllAsync(ct);
        var result = new List<AdminUserResponse>(users.Count);
        foreach (var user in users)
        {
            var role = await _userRepository.GetPrimaryRoleAsync(user);
            result.Add(user.ToAdminDto(role));
        }
        return result;
    }

    public async Task<ServiceResult<AdminUserResponse>> GetUserByIdAsync(int id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null)
            return ServiceError.NotFound(ErrorMessages.UserNotFound);

        var role = await _userRepository.GetPrimaryRoleAsync(user);
        return user.ToAdminDto(role);
    }

    public async Task<ServiceResult<AdminUserResponse>> CreateUserAsync(AdminCreateUserRequest request, CancellationToken ct = default)
    {
        if (!ValidRoles.Contains(request.Role))
            return new ServiceError(ErrorMessages.InvalidRole(string.Join(", ", ValidRoles)));

        var normalizedEmail = request.Email.ToUpperInvariant();
        if (await _userRepository.EmailExistsAsync(normalizedEmail, ct))
            return new ServiceError(ErrorMessages.EmailAlreadyUsed);

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Customer = new Customer()
        };

        var (created, errors) = await _userRepository.CreateAsync(user, request.Password);
        if (!created)
            return new ServiceError(string.Join(", ", errors));

        var (roleOk, roleErrors) = await _userRepository.AddToRoleAsync(user, request.Role);
        if (!roleOk)
            return new ServiceError(string.Join(", ", roleErrors));

        return user.ToAdminDto(request.Role);
    }

    public async Task<ServiceResult<AdminUserResponse>> UpdateUserAsync(
        int id, AdminUpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null)
            return ServiceError.NotFound(ErrorMessages.UserNotFound);

        if (!ValidRoles.Contains(request.Role))
            return new ServiceError(ErrorMessages.InvalidRole(string.Join(", ", ValidRoles)));

        if (!Enum.TryParse<UserStatus>(request.Status, ignoreCase: true, out var newStatus))
            return new ServiceError(ErrorMessages.InvalidStatus(string.Join(", ", Enum.GetNames<UserStatus>())));

        var normalizedEmail = request.Email.ToUpperInvariant();
        if (await _userRepository.EmailExistsExceptAsync(normalizedEmail, id, ct))
            return new ServiceError(ErrorMessages.EmailAlreadyUsed);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.UserName = request.Email;
        user.NormalizedEmail = normalizedEmail;
        user.NormalizedUserName = normalizedEmail;
        user.Status = newStatus;
        user.UpdatedAt = DateTime.UtcNow;

        var currentRoles = await _userRepository.GetRolesAsync(user);
        if (currentRoles.FirstOrDefault() != request.Role)
        {
            if (currentRoles.Count > 0)
                await _userRepository.RemoveFromRolesAsync(user, currentRoles);
            var (roleOk, roleErrors) = await _userRepository.AddToRoleAsync(user, request.Role);
            if (!roleOk)
                return new ServiceError(string.Join(", ", roleErrors));
        }

        await _userRepository.SaveChangesAsync(ct);
        return user.ToAdminDto(request.Role);
    }

    public async Task<ServiceResult> DeleteUserAsync(int id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null)
            return ServiceError.NotFound(ErrorMessages.UserNotFound);

        var (succeeded, errors) = await _userRepository.DeleteAsync(user);
        if (!succeeded)
            return new ServiceError(string.Join(", ", errors));

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<AdminUserResponse>> UpdateUserRoleAsync(
        int id, UpdateUserRoleRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null)
            return ServiceError.NotFound(ErrorMessages.UserNotFound);

        if (!ValidRoles.Contains(request.Role))
            return new ServiceError(ErrorMessages.InvalidRole(string.Join(", ", ValidRoles)));

        var currentRoles = await _userRepository.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            await _userRepository.RemoveFromRolesAsync(user, currentRoles);

        var (roleOk, roleErrors) = await _userRepository.AddToRoleAsync(user, request.Role);
        if (!roleOk)
            return new ServiceError(string.Join(", ", roleErrors));

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync(ct);
        return user.ToAdminDto(request.Role);
    }

    public async Task<ServiceResult<AdminUserResponse>> UpdateUserStatusAsync(
        int id, UpdateUserStatusRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<UserStatus>(request.Status, ignoreCase: true, out var newStatus))
            return new ServiceError(ErrorMessages.InvalidStatus(string.Join(", ", Enum.GetNames<UserStatus>())));

        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null)
            return ServiceError.NotFound(ErrorMessages.UserNotFound);

        user.Status = newStatus;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync(ct);

        var role = await _userRepository.GetPrimaryRoleAsync(user);
        return user.ToAdminDto(role);
    }
}
