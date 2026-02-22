using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DeliverTableServer.Configuration;
using DeliverTableServer.Data;
using DeliverTableServer.Mappers;
using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DeliverTableContext _context;
    private readonly JwtConfig _jwtConfig;

    public AuthController(DeliverTableContext context, JwtConfig jwtConfig)
    {
        _context = context;
        _jwtConfig = jwtConfig;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Identifiants invalides" });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
            return Unauthorized(new { Error = "Identifiants invalides" });

        var token = GenerateJwt(user);

        return Ok(new ConnectionResponse{ Token = token, User =
        new UserResponse {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role.ToString()
        } });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Champs Invalides" });

        // Vérifier si l'email existe déjà
        if (await _context.Users.AnyAsync(u => u.Email == registerRequest.Email))
            return BadRequest(new { Error = "Email déjà utilisé" });

        // Hash du mot de passe
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password);

        var user = new User
        {
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            Email = registerRequest.Email,
            PasswordHash = passwordHash
        };

        user.CustomerProfile = new CustomerProfile();

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        var token = GenerateJwt(user);

        return Ok(new ConnectionResponse { Token = token, User = new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString()
        } });
    }

    [HttpPost("restaurant/register")]
    public async Task<IActionResult> RegisterRestaurant([FromBody] RestaurantRegister registerRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Identifiants invalides" });
        // Vérifier si l'email existe déjà
        if (await _context.Users.AnyAsync(u => u.Email == registerRequest.Email))
            return BadRequest(new { Error = "Cet email est déjà utilisé" });
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password);

        var user = new User
        {
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            Email = registerRequest.Email,
            Role = UserRole.RestaurantOwner,
            PasswordHash = passwordHash
        };

        user.RestaurantOwner = new RestaurantOwner
        {
            CompanyName = registerRequest.CompanyName,
            VatNumber = registerRequest.VatNumber,
            ContactPhoneNumber = registerRequest.ContactPhoneNumber
        };
        
        user.CustomerProfile = new CustomerProfile();

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        var token = GenerateJwt(user);
        return Ok(new ConnectionResponse
        {
            Token = token, User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString()
            }
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim))
            return Unauthorized(new { Error = "Token invalide ou expiré" });

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { Error = "Token invalide" });

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { Error = "Utilisateur introuvable" });

        return Ok(user.ToDto());
    }

    private string GenerateJwt(User user)
    {
        var key = Encoding.UTF8.GetBytes(_jwtConfig.Key);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", user.Role.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.ExpireMinutes),
            Issuer = _jwtConfig.Issuer,
            Audience = _jwtConfig.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        Console.WriteLine(token);
        return tokenHandler.WriteToken(token);
    }
}