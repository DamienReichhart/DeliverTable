using DeliverTableServer.Common;
using DeliverTableInfrastructure.Data;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Services;

public sealed class AdminDashboardService(DeliverTableContext dbContext) : IAdminDashboardService
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<ServiceResult<AdminDashboardStatsResponse>> GetStatsAsync(CancellationToken ct = default)
    {
        var stats = new AdminDashboardStatsResponse
        {
            TotalUsers = await _dbContext.Users.CountAsync(ct),
            TotalRestaurants = await _dbContext.Restaurants.CountAsync(ct),
            TotalOrders = await _dbContext.Orders.CountAsync(ct),
            TotalRevenue = await _dbContext.Orders.SumAsync(o => o.TotalAmount, ct),
            ActivePromotions = await _dbContext.Promotions.CountAsync(p => p.IsActive, ct),
            PendingOrders = await _dbContext.Orders.CountAsync(o => o.Status == OrderStatus.Pending, ct)
        };
        return stats;
    }

    public async Task<ServiceResult<AdminDashboardAnalyticsResponse>> GetAnalyticsAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var thirtyDaysAgo = today.AddDays(-29);
        var weekAgo = today.AddDays(-7);

        var recentOrders = await _dbContext.Orders
            .Where(o => o.CreatedAt >= thirtyDaysAgo)
            .ToListAsync(ct);

        var revenueByDay = recentOrders
            .GroupBy(o => DateOnly.FromDateTime(o.CreatedAt))
            .Select(g => new DailyRevenuePoint
            {
                Date = g.Key,
                Amount = g.Sum(o => o.TotalAmount)
            })
            .OrderBy(p => p.Date)
            .ToList();

        var ordersByDay = recentOrders
            .GroupBy(o => DateOnly.FromDateTime(o.CreatedAt))
            .Select(g => new DailyCountPoint
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(p => p.Date)
            .ToList();

        var recentUsers = await _dbContext.Users
            .Where(u => u.CreatedAt >= thirtyDaysAgo)
            .ToListAsync(ct);

        var userRegistrationsByDay = recentUsers
            .GroupBy(u => DateOnly.FromDateTime(u.CreatedAt))
            .Select(g => new DailyCountPoint
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(p => p.Date)
            .ToList();

        var allOrders = await _dbContext.Orders.ToListAsync(ct);
        var totalOrderCount = allOrders.Count;

        var ordersByStatus = Breakdown(allOrders, o => o.Status, totalOrderCount);
        var ordersByType = Breakdown(allOrders, o => o.OrderType, totalOrderCount);
        var paymentsByStatus = Breakdown(allOrders, o => o.PaymentStatus, totalOrderCount);

        var topRestaurants = await _dbContext.Orders
            .Include(o => o.Restaurant)
            .GroupBy(o => new { o.RestaurantId, o.Restaurant.Name })
            .Select(g => new TopRestaurantItem
            {
                RestaurantId = g.Key.RestaurantId,
                Name = g.Key.Name,
                Revenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderByDescending(r => r.Revenue)
            .Take(5)
            .ToListAsync(ct);

        var latestOrders = await _dbContext.Orders
            .Include(o => o.Customer)
            .Include(o => o.Restaurant)
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new RecentOrderItem
            {
                OrderId = o.Id,
                CustomerName = o.Customer.FirstName + " " + o.Customer.LastName,
                RestaurantName = o.Restaurant.Name,
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString(),
                OrderType = o.OrderType.ToString(),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(ct);

        var averageOrderValue = totalOrderCount > 0
            ? Math.Round(allOrders.Average(o => o.TotalAmount), 2)
            : 0;

        var todayOrders = recentOrders.Where(o => o.CreatedAt.Date == today).ToList();
        var newUsersThisWeek = recentUsers.Count(u => u.CreatedAt >= weekAgo);

        var analytics = new AdminDashboardAnalyticsResponse
        {
            RevenueByDay = revenueByDay,
            OrdersByDay = ordersByDay,
            UserRegistrationsByDay = userRegistrationsByDay,
            OrdersByStatus = ordersByStatus,
            OrdersByType = ordersByType,
            PaymentsByStatus = paymentsByStatus,
            TopRestaurantsByRevenue = topRestaurants,
            RecentOrders = latestOrders,
            AverageOrderValue = averageOrderValue,
            TodayRevenue = todayOrders.Sum(o => o.TotalAmount),
            TodayOrders = todayOrders.Count,
            NewUsersThisWeek = newUsersThisWeek
        };

        return analytics;
    }

    private static List<StatusBreakdownItem> Breakdown<TKey>(
        IReadOnlyCollection<DeliverTableInfrastructure.Models.Order> orders,
        Func<DeliverTableInfrastructure.Models.Order, TKey> key,
        int total) where TKey : notnull
    {
        return orders
            .GroupBy(key)
            .Select(g => new StatusBreakdownItem
            {
                Label = g.Key.ToString() ?? string.Empty,
                Count = g.Count(),
                Percentage = total > 0 ? Math.Round((decimal)g.Count() / total * 100, 1) : 0
            })
            .OrderByDescending(s => s.Count)
            .ToList();
    }
}
