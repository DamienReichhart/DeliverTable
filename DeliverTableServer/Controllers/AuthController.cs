using System.Security.Claims;
using DeliverTableServer.Constants;
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Auth.Base)]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost(ApiRoutes.Auth.LoginRoute)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Auth.RegisterRoute)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Auth.RestaurantRegisterRoute)]
    public async Task<IActionResult> RegisterRestaurant([FromBody] RestaurantRegister request, CancellationToken ct)
    {
        var result = await _authService.RegisterRestaurantAsync(request, ct);
        return result.ToOkResult();
    }

    [Authorize]
    [HttpGet(ApiRoutes.Auth.MeRoute)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        if (userId is null)
            return Unauthorized(new { Error = ErrorMessages.InvalidOrExpiredToken });

        var result = await _authService.GetProfileAsync(userId.Value, ct);
        return result.ToOkResult();
    }

    [Authorize]
    [HttpPut(ApiRoutes.Auth.UpdateProfileRoute)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        if (userId is null)
            return Unauthorized(new { Error = ErrorMessages.InvalidOrExpiredToken });

        var result = await _authService.UpdateProfileAsync(userId.Value, request, ct);
        return result.ToOkResult();
    }

    [Authorize]
    [HttpPut(ApiRoutes.Auth.ChangePasswordRoute)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        if (userId is null)
            return Unauthorized(new { Error = ErrorMessages.InvalidOrExpiredToken });

        var result = await _authService.ChangePasswordAsync(userId.Value, request, ct);
        if (!result.IsSuccess)
            return result.Error!.ToErrorResult();

        return Ok(new { Message = ErrorMessages.PasswordChangedSuccessfully });
    }

    private int? GetAuthenticatedUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out var userId))
            return null;
        return userId;
    }
}
