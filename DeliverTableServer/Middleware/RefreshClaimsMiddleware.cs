using System.Security.Claims;
using DeliverTableServer.Constants;
using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Identity;

namespace DeliverTableServer.Middleware;

/// <summary>
///     Replaces JWT role claims with the current database role on every authenticated request.
///     Also rejects suspended/banned users so that status changes take effect immediately.
/// </summary>
public sealed class RefreshClaimsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, UserManager<User> userManager)
    {
        var identity = context.User.Identity;
        if (identity is not { IsAuthenticated: true })
        {
            await next(context);
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            await next(context);
            return;
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (user.Status is UserStatus.Suspended or UserStatus.Banned)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = ErrorMessages.AccountSuspendedOrBanned });
            return;
        }

        var roles = await userManager.GetRolesAsync(user);
        var currentRole = roles.FirstOrDefault();

        var claimsIdentity = (ClaimsIdentity)identity;

        var existingRoleClaims = claimsIdentity.FindAll(ClaimTypes.Role).ToList();
        foreach (var claim in existingRoleClaims)
        {
            claimsIdentity.RemoveClaim(claim);
        }

        if (currentRole is not null)
        {
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, currentRole));
        }

        await next(context);
    }
}
