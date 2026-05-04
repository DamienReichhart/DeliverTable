using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Extensions;

public static class OrderQueryExtensions
{
    public static IQueryable<Order> IncludeOrderAggregate(this IQueryable<Order> query) =>
        query
            .Include(o => o.Items)
            .Include(o => o.Restaurant)
            .Include(o => o.Discounts)
            .Include(o => o.Payments)
            .Include(o => o.RestaurantTable)
            .Include(o => o.Event);
}
