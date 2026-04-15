using DeliverTableInfrastructure.Models;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

/// <summary>
///     Unified data-access abstraction for <see cref="User"/> entities.
///     Wraps both <c>UserManager</c> and <c>DbContext</c> user operations
///     behind a single testable interface.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default);
    Task<bool> EmailExistsExceptAsync(string normalizedEmail, int excludeUserId, CancellationToken ct = default);
    Task<List<User>> GetAllAsync(CancellationToken ct = default);
    Task<List<User>> ListByRoleAsync(string roleName, CancellationToken ct = default);

    Task<(bool Succeeded, IEnumerable<string> Errors)> CreateAsync(User user, string password);
    Task<(bool Succeeded, IEnumerable<string> Errors)> DeleteAsync(User user);
    Task<(bool Succeeded, IEnumerable<string> Errors)> AddToRoleAsync(User user, string role);
    Task<(bool Succeeded, IEnumerable<string> Errors)> RemoveFromRolesAsync(User user, IList<string> roles);
    Task<IList<string>> GetRolesAsync(User user);
    Task<string?> GetPrimaryRoleAsync(User user);
    Task<bool> CheckPasswordAsync(User user, string password);
    Task<(bool Succeeded, IEnumerable<string> Errors)> ChangePasswordAsync(User user, string currentPassword, string newPassword);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
