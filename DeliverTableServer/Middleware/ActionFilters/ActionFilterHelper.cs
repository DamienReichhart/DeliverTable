using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DeliverTableServer.Middleware.ActionFilters;

public static class ActionFilterHelper
{
    public static bool TryGetRouteId(
        ActionExecutingContext context, out int id, string errorMessage)
    {
        var routeId = context.RouteData.Values["id"]?.ToString();
        if (routeId is not null && int.TryParse(routeId, out id))
            return true;

        id = 0;
        context.Result = new BadRequestObjectResult(new { Error = errorMessage });
        return false;
    }

    public static bool TryGetUserId(ActionExecutingContext context, out int userId)
    {
        var userIdString = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString is not null && int.TryParse(userIdString, out userId))
            return true;

        userId = 0;
        context.Result = new UnauthorizedResult();
        return false;
    }
}
