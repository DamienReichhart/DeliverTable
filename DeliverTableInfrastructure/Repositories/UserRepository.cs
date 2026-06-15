using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class UserRepository(
    DeliverTableContext dbContext,
    UserManager<User> userManager
) : IUserRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;
    private readonly UserManager<User> _userManager = userManager;

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default)
        => await _dbContext.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);

    public async Task<bool> EmailExistsExceptAsync(string normalizedEmail, int excludeUserId, CancellationToken ct = default)
        => await _dbContext.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail && u.Id != excludeUserId, ct);

    public async Task<List<User>> GetAllAsync(CancellationToken ct = default)
        => await _dbContext.Users.OrderBy(u => u.Id).ToListAsync(ct);

    public async Task<List<User>> ListByRoleAsync(string roleName, CancellationToken ct = default)
    {
        string normalizedRole = roleName.ToUpperInvariant();
        IQueryable<User> query =
            from user in _dbContext.Users
            join userRole in _dbContext.UserRoles on user.Id equals userRole.UserId
            join role in _dbContext.Roles on userRole.RoleId equals role.Id
            where role.NormalizedName == normalizedRole
            select user;

        return await query.OrderBy(u => u.Id).ToListAsync(ct);
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> CreateAsync(User user, string password)
    {
        IdentityResult result = await _userManager.CreateAsync(user, password);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> DeleteAsync(User user)
    {
        IdentityResult result = await _userManager.DeleteAsync(user);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> AddToRoleAsync(User user, string role)
    {
        IdentityResult result = await _userManager.AddToRoleAsync(user, role);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> RemoveFromRolesAsync(User user, IList<string> roles)
    {
        IdentityResult result = await _userManager.RemoveFromRolesAsync(user, roles);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<IList<string>> GetRolesAsync(User user)
        => await _userManager.GetRolesAsync(user);

    public async Task<string?> GetPrimaryRoleAsync(User user)
    {
        IList<string> roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault();
    }

    public async Task<bool> CheckPasswordAsync(User user, string password)
        => await _userManager.CheckPasswordAsync(user, password);

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> ChangePasswordAsync(
        User user, string currentPassword, string newPassword)
    {
        IdentityResult result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _dbContext.SaveChangesAsync(ct);
}
