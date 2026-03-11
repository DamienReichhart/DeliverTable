using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DeliverTableServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DeliverTableServer.Middleware.ActionFilters
{
    public class RestaurantOwnerAttribute : TypeFilterAttribute
    {
        public RestaurantOwnerAttribute() : base(typeof(RestaurantOwnerAttribute))
        {
        }

        private sealed class RestaurantOwnerFilter(DeliverTableContext dbContext) : IAsyncActionFilter
        {
            private readonly DeliverTableContext _dbContext = dbContext;
            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var dishId = context.RouteData.Values["id"]?.ToString();
                if (dishId == null || !int.TryParse(dishId, out int id))
                {
                    context.Result = new BadRequestObjectResult(new { Error = "ID de plat manquant ou invalide." });
                    return;
                }
                var dish = await _dbContext.Dishes.FindAsync(id);
                if (dish == null)
                {
                    context.Result = new NotFoundResult();
                    return;
                }
                int? restaurantOwnerId = await _dbContext.Restaurants
                    .Where(r => r.Id == dish.RestaurantId)
                    .Select(r => r.OwnerId)
                    .FirstOrDefaultAsync();
                if (restaurantOwnerId == null)
                {
                    context.Result = new NotFoundResult();
                    return;
                }
                string? currentUserId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (currentUserId == null || !int.TryParse(currentUserId, out int userId))
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }
                if (restaurantOwnerId != userId)
                {
                    context.Result = new ForbidResult();
                    return;
                }
                await next();
            }
        }
    }
}