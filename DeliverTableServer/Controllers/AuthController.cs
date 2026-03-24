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
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        var result = await _authService.GetProfileAsync(userId, ct);
        return result.ToOkResult();
    }

    [Authorize]
    [HttpPut(ApiRoutes.Auth.UpdateProfileRoute)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        var result = await _authService.UpdateProfileAsync(userId, request, ct);
        return result.ToOkResult();
    }

    [Authorize]
    [HttpPut(ApiRoutes.Auth.ChangePasswordRoute)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();

        var result = await _authService.ChangePasswordAsync(userId, request, ct);
        return result.ToOkMessageResult(ErrorMessages.PasswordChangedSuccessfully);
    }
}
