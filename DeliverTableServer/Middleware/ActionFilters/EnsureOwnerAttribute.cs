using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Middleware.ActionFilters;

public class EnsureOwnerAttribute : TypeFilterAttribute
{
    public EnsureOwnerAttribute() : base(typeof(EnsureOwnerFilter)) { }

    private sealed class EnsureOwnerFilter(DeliverTableContext dbContext) : IAsyncActionFilter
    {
        private readonly DeliverTableContext _dbContext = dbContext;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!ActionFilterHelper.TryGetRouteId(context, out int id, ErrorMessages.MissingOrInvalidRestaurantId))
                return;

            if (!ActionFilterHelper.TryGetUserId(context, out int currentUserId))
                return;

            int? restaurantOwnerId = await _dbContext.Restaurants
                .Where(r => r.Id == id)
                .Select(r => (int?)r.OwnerId)
                .FirstOrDefaultAsync();

            if (restaurantOwnerId is null)
            {
                context.Result = new NotFoundResult();
                return;
            }

            if (restaurantOwnerId != currentUserId)
            {
                context.Result = new ForbidResult();
                return;
            }

            await next();
        }
    }
}
