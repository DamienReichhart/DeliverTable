using DeliverTableServer.Data;
using DeliverTableServer.Mappers;
using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Enums;
using DeliverTableSharedLibrary.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Admin.Base)]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminController(
    DeliverTableContext context,
    UserManager<User> userManager
) : ControllerBase
{
    private static readonly string[] ValidRoles = Enum.GetNames<UserRole>();

    private readonly DeliverTableContext _context = context;
    private readonly UserManager<User> _userManager = userManager;

    [HttpGet(ApiRoutes.Admin.UsersRoute)]
    public async Task<IActionResult> GetAllUsers(CancellationToken ct)
    {
        var users = await _context.Users
            .OrderBy(u => u.Id)
            .ToListAsync(ct);

        var result = new List<AdminUserResponse>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(user.ToAdminDto(roles.FirstOrDefault()));
        }

        return Ok(result);
    }

    [HttpGet(ApiRoutes.Admin.UserByIdRoute)]
    public async Task<IActionResult> GetUserById(int id, CancellationToken ct)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return NotFound(new { Error = "Utilisateur introuvable" });

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(user.ToAdminDto(roles.FirstOrDefault()));
    }

    [HttpPost(ApiRoutes.Admin.UsersRoute)]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Champs invalides" });

        if (!ValidRoles.Contains(request.Role))
            return BadRequest(new { Error = $"Rôle invalide. Valeurs possibles : {string.Join(", ", ValidRoles)}" });

        var normalizedEmail = request.Email.ToUpperInvariant();
        if (await _context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct))
            return BadRequest(new { Error = "Cette adresse email est déjà utilisée" });

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Customer = new Customer()
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(new { Error = string.Join(", ", createResult.Errors.Select(e => e.Description)) });

        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
            return BadRequest(new { Error = string.Join(", ", roleResult.Errors.Select(e => e.Description)) });

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user.ToAdminDto(request.Role));
    }

    [HttpPut(ApiRoutes.Admin.UserByIdRoute)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] AdminUpdateUserRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Champs invalides" });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return NotFound(new { Error = "Utilisateur introuvable" });

        if (!ValidRoles.Contains(request.Role))
            return BadRequest(new { Error = $"Rôle invalide. Valeurs possibles : {string.Join(", ", ValidRoles)}" });

        if (!Enum.TryParse<UserStatus>(request.Status, ignoreCase: true, out var newStatus))
            return BadRequest(new { Error = $"Statut invalide. Valeurs possibles : {string.Join(", ", Enum.GetNames<UserStatus>())}" });

        var normalizedEmail = request.Email.ToUpperInvariant();
        if (user.NormalizedEmail != normalizedEmail
            && await _context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct))
        {
            return BadRequest(new { Error = "Cette adresse email est déjà utilisée" });
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.UserName = request.Email;
        user.NormalizedEmail = normalizedEmail;
        user.NormalizedUserName = normalizedEmail;
        user.Status = newStatus;
        user.UpdatedAt = DateTime.UtcNow;

        var currentRoles = await _userManager.GetRolesAsync(user);
        var currentRole = currentRoles.FirstOrDefault();

        if (currentRole != request.Role)
        {
            if (currentRoles.Count > 0)
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

            var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
            if (!roleResult.Succeeded)
                return BadRequest(new { Error = string.Join(", ", roleResult.Errors.Select(e => e.Description)) });
        }

        await _context.SaveChangesAsync(ct);

        return Ok(user.ToAdminDto(request.Role));
    }

    [HttpDelete(ApiRoutes.Admin.UserByIdRoute)]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken ct)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return NotFound(new { Error = "Utilisateur introuvable" });

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { Error = string.Join(", ", result.Errors.Select(e => e.Description)) });

        return NoContent();
    }

    [HttpPut(ApiRoutes.Admin.UserByIdRoleRoute)]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Rôle invalide" });

        var user = await _context.Users.FindAsync(id);
        if (user is null)
            return NotFound(new { Error = "Utilisateur introuvable" });

        if (!ValidRoles.Contains(request.Role))
            return BadRequest(new { Error = $"Rôle invalide. Valeurs possibles : {string.Join(", ", ValidRoles)}" });

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

        var result = await _userManager.AddToRoleAsync(user, request.Role);
        if (!result.Succeeded)
            return BadRequest(new { Error = string.Join(", ", result.Errors.Select(e => e.Description)) });

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(user.ToAdminDto(request.Role));
    }

    [HttpPut(ApiRoutes.Admin.UserByIdStatusRoute)]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Statut invalide" });

        if (!Enum.TryParse<UserStatus>(request.Status, ignoreCase: true, out var newStatus))
            return BadRequest(new { Error = $"Statut invalide. Valeurs possibles : {string.Join(", ", Enum.GetNames<UserStatus>())}" });

        var user = await _context.Users.FindAsync(id);
        if (user is null)
            return NotFound(new { Error = "Utilisateur introuvable" });

        user.Status = newStatus;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(user.ToAdminDto(roles.FirstOrDefault()));
    }
}
