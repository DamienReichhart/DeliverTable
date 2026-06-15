using System.Security.Claims;
using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Identity;
using System.Security.Principal;

namespace DeliverTableServer.Middleware;

/// <summary>
///     Replaces JWT role claims with the current database role on every authenticated request.
///     Also rejects suspended/banned users so that status changes take effect immediately.
/// </summary>
public sealed class RefreshClaimsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, UserManager<User> userManager)
    {
        IIdentity? identity = context.User.Identity;
        if (identity is not { IsAuthenticated: true })
        {
            await next(context);
            return;
        }

        string? userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId))
        {
            await next(context);
            return;
        }

        User? user = await userManager.FindByIdAsync(userId.ToString());
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

        IList<string> roles = await userManager.GetRolesAsync(user);
        string? currentRole = roles.FirstOrDefault();

        ClaimsIdentity claimsIdentity = (ClaimsIdentity)identity;

        List<Claim> existingRoleClaims = claimsIdentity.FindAll(ClaimTypes.Role).ToList();
        foreach (Claim claim in existingRoleClaims)
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
