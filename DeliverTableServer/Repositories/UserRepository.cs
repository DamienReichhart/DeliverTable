using DeliverTableServer.Data;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories;

public sealed class UserRepository(
    DeliverTableContext context,
    UserManager<User> userManager
) : IUserRepository
{
    private readonly DeliverTableContext _context = context;
    private readonly UserManager<User> _userManager = userManager;

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default)
        => await _context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);

    public async Task<bool> EmailExistsExceptAsync(string normalizedEmail, int excludeUserId, CancellationToken ct = default)
        => await _context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail && u.Id != excludeUserId, ct);

    public async Task<List<User>> GetAllAsync(CancellationToken ct = default)
        => await _context.Users.OrderBy(u => u.Id).ToListAsync(ct);

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> CreateAsync(User user, string password)
    {
        var result = await _userManager.CreateAsync(user, password);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> DeleteAsync(User user)
    {
        var result = await _userManager.DeleteAsync(user);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> AddToRoleAsync(User user, string role)
    {
        var result = await _userManager.AddToRoleAsync(user, role);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> RemoveFromRolesAsync(User user, IList<string> roles)
    {
        var result = await _userManager.RemoveFromRolesAsync(user, roles);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<IList<string>> GetRolesAsync(User user)
        => await _userManager.GetRolesAsync(user);

    public async Task<string?> GetPrimaryRoleAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault();
    }

    public async Task<bool> CheckPasswordAsync(User user, string password)
        => await _userManager.CheckPasswordAsync(user, password);

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> ChangePasswordAsync(
        User user, string currentPassword, string newPassword)
    {
        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
