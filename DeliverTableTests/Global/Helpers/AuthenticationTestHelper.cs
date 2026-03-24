using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableTests.Global.Helpers;

public static class AuthenticationTestHelper
{
    public static void SetupAuthenticatedUser(
        ControllerBase controller, string userId, string? role = null)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(userId))
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        if (!string.IsNullOrEmpty(role))
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "TestScheme");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
