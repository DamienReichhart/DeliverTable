using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Extensions;

public static class ControllerExtensions
{
    public static bool TryGetUserId(this ControllerBase controller, out int userId)
    {
        return int.TryParse(
            controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
    }
}
