using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DeliverTableServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Middleware.ActionFilters
{
    public class EnsureOwnerAttribute : TypeFilterAttribute
    {
        public EnsureOwnerAttribute() : base(typeof(EnsureOwnerFilter))
        {
        }

        private sealed class EnsureOwnerFilter(DeliverTableContext dbcontext) : IAsyncActionFilter
        {
            private readonly DeliverTableContext _dbContext = dbcontext;
            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                // 1. Get the Restaurant ID from the route parameters
                if (!context.ActionArguments.TryGetValue("id", out var routeId) || routeId is not int restaurantId)
                {
                    context.Result = new BadRequestObjectResult(new { Error = "ID de restaurant manquant ou invalide." });
                    return;
                }

                // 2. Get the Connected User ID from Claims
                var userIdString = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out int currentUserId))
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                // 3. Check database ownership
                var restaurantOwnerId = await _dbContext.Restaurants
                    .Where(r => r.Id == restaurantId)
                    .Select(r => (int?)r.OwnerId)
                    .FirstOrDefaultAsync();

                if (restaurantOwnerId == null)
                {
                    context.Result = new NotFoundResult();
                    return;
                }

                if (restaurantOwnerId != currentUserId)
                {
                    context.Result = new ForbidResult(); // 403 Forbidden
                    return;
                }

                await next();
            }
        }
    }
}