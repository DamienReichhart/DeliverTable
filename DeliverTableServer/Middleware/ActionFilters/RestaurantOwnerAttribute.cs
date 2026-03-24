using DeliverTableServer.Constants;
using DeliverTableServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Middleware.ActionFilters;

public class RestaurantOwnerAttribute : TypeFilterAttribute
{
    public RestaurantOwnerAttribute() : base(typeof(RestaurantOwnerFilter)) { }

    private sealed class RestaurantOwnerFilter(DeliverTableContext dbContext) : IAsyncActionFilter
    {
        private readonly DeliverTableContext _dbContext = dbContext;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!ActionFilterHelper.TryGetRouteId(context, out int id, ErrorMessages.MissingOrInvalidDishId))
                return;

            var dish = await _dbContext.Dishes.FindAsync(id);
            if (dish is null)
            {
                context.Result = new NotFoundResult();
                return;
            }

            var restaurantOwnerId = await _dbContext.Restaurants
                .Where(r => r.Id == dish.RestaurantId)
                .Select(r => (int?)r.OwnerId)
                .FirstOrDefaultAsync();

            if (restaurantOwnerId is null)
            {
                context.Result = new NotFoundResult();
                return;
            }

            if (!ActionFilterHelper.TryGetUserId(context, out int userId))
                return;

            if (restaurantOwnerId != userId)
            {
                context.Result = new ForbidResult();
                return;
            }

            await next();
        }
    }
}
