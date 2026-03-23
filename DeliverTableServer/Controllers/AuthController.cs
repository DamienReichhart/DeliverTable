using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DeliverTableServer.Configuration;
using DeliverTableServer.Data;
using DeliverTableServer.Mappers;
using DeliverTableServer.Models;
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Auth.Base)]
public class AuthController(
    DeliverTableContext context,
    ITokenService tokenService,
    UserManager<User> userManager,
    IHostEnvironment env
    ) : ControllerBase
{
    private readonly string _defaultRoleValue = "Customer";
    private readonly DeliverTableContext _context = context;
    private readonly ITokenService _tokenService = tokenService;
    private readonly UserManager<User> _userManager = userManager;
    private readonly IHostEnvironment _env = env;

    [HttpPost(ApiRoutes.Auth.LoginRoute)]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Identifiants invalides" });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);

        if (user == null || !await _userManager.CheckPasswordAsync(user, loginRequest.Password))
            return Unauthorized(new { Error = "Identifiants invalides" });

        if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Banned)
            return Unauthorized(new { Error = "Compte suspendu ou banni" });

        var token = await _tokenService.CreateToken(user);
        var userRoles = await _userManager.GetRolesAsync(user);
        var role = userRoles.FirstOrDefault(_defaultRoleValue);

        return Ok(new ConnectionResponse
        {
            Token = token,
            User = user.ToDto(role)
        });
    }

    [HttpPost(ApiRoutes.Auth.RegisterRoute)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Champs Invalides" });

        var normalizedEmail = registerRequest.Email?.ToUpperInvariant();
        if (await _context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail))
            return BadRequest(new { Error = "Email déjà utilisé" });

        User user = new()
        {
            UserName = registerRequest.Email,
            Email = registerRequest.Email,
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            Customer = new Customer()
        };

        var (createdUser, errors) = await CreateUser(user, registerRequest.Password, "Customer");
        if (createdUser == null) return BadRequest(
            _env.IsDevelopment() ?
            new { Errors = errors }
            :
            new { Errors = new[] { "Une erreur est survenue" } }
            );

        var userRoles = await _userManager.GetRolesAsync(user);
        var role = userRoles.FirstOrDefault(_defaultRoleValue);
        var token = await _tokenService.CreateToken(user);

        return Ok(new ConnectionResponse
        {
            Token = token,
            User = user.ToDto(role)
        });
    }

    [HttpPost(ApiRoutes.Auth.RestaurantRegisterRoute)]
    public async Task<IActionResult> RegisterRestaurant([FromBody] RestaurantRegister registerRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Identifiants invalides" });

        var normalizedEmail = registerRequest.Email?.ToUpperInvariant();
        if (await _context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail))
            return BadRequest(new { Error = "Cet email est déjà utilisé" });

        User user = new()
        {
            UserName = registerRequest.Email,
            Email = registerRequest.Email,
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            RestaurantOwner = new RestaurantOwner
            {
                CompanyName = registerRequest.CompanyName,
                VatNumber = registerRequest.VatNumber,
                ContactPhoneNumber = registerRequest.ContactPhoneNumber
            },
            Customer = new Customer()
        };

        var (createdUser, errors) = await CreateUser(user, registerRequest.Password, "Restaurant_Owner");
        if (createdUser == null) return BadRequest(
            _env.IsDevelopment() ?
            new { Errors = errors }
            :
            new { Errors = new[] { "Une erreur est survenue" } }
            );

        var userRoles = await _userManager.GetRolesAsync(user);
        var role = userRoles.FirstOrDefault("Restaurant_Owner");
        var token = await _tokenService.CreateToken(user);

        return Ok(new ConnectionResponse
        {
            Token = token,
            User = user.ToDto(role)
        });
    }

    [Authorize]
    [HttpGet(ApiRoutes.Auth.MeRoute)]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { Error = "Token invalide ou expiré" });

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { Error = "Utilisateur introuvable" });

        var userRoles = await _userManager.GetRolesAsync(user);
        var role = userRoles.FirstOrDefault(_defaultRoleValue);

        return Ok(user.ToDto(role));
    }

    [Authorize]
    [HttpPut(ApiRoutes.Auth.UpdateProfileRoute)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Champs invalides" });

        var user = await GetAuthenticatedUser();
        if (user == null)
            return Unauthorized(new { Error = "Token invalide ou expiré" });

        var normalizedEmail = request.Email?.ToUpperInvariant();
        if (user.NormalizedEmail != normalizedEmail
            && await _context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail))
        {
            return BadRequest(new { Error = "Cette adresse email est déjà utilisée" });
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.UserName = request.Email;
        user.NormalizedEmail = normalizedEmail;
        user.NormalizedUserName = normalizedEmail;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var userRoles = await _userManager.GetRolesAsync(user);
        var role = userRoles.FirstOrDefault(_defaultRoleValue);
        var token = await _tokenService.CreateToken(user);

        return Ok(new ConnectionResponse
        {
            Token = token,
            User = user.ToDto(role)
        });
    }

    [Authorize]
    [HttpPut(ApiRoutes.Auth.ChangePasswordRoute)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Champs invalides" });

        var user = await GetAuthenticatedUser();
        if (user == null)
            return Unauthorized(new { Error = "Token invalide ou expiré" });

        if (!await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
            return BadRequest(new { Error = "Le mot de passe actuel est incorrect" });

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { Error = string.Join(", ", errors) });
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Mot de passe modifié avec succès" });
    }

    private async Task<User?> GetAuthenticatedUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        return await _context.Users.FindAsync(userId);
    }

    private async Task<(User? user, IEnumerable<string> errors)> CreateUser(User user, string password, string role = "Customer")
    {
        try
        {
            var createdUser = await _userManager.CreateAsync(user, password);
            if (createdUser.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, role);
                if (roleResult.Succeeded)
                {
                    return (user, []);
                }
            }
            return (null, createdUser.Errors.Select(e => e.Description));
        }
        catch (Exception exception)
        {
            return (null, [exception.Message]);
        }
    }
}