using DeliverTableServer.Common;
using DeliverTableInfrastructure.Data;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;
using DeliverTableInfrastructure.Models;

namespace DeliverTableServer.Services;

public sealed class AdminDashboardService(DeliverTableContext dbContext) : IAdminDashboardService
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<ServiceResult<AdminDashboardStatsResponse>> GetStatsAsync(CancellationToken ct = default)
    {
        AdminDashboardStatsResponse stats = new AdminDashboardStatsResponse
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
        DateTime today = DateTime.UtcNow.Date;
        DateTime thirtyDaysAgo = today.AddDays(-29);
        DateTime weekAgo = today.AddDays(-7);

        List<Order> recentOrders = await _dbContext.Orders
            .Where(o => o.CreatedAt >= thirtyDaysAgo)
            .ToListAsync(ct);

        List<DailyRevenuePoint> revenueByDay = recentOrders
            .GroupBy(o => DateOnly.FromDateTime(o.CreatedAt))
            .Select(g => new DailyRevenuePoint
            {
                Date = g.Key,
                Amount = g.Sum(o => o.TotalAmount)
            })
            .OrderBy(p => p.Date)
            .ToList();

        List<DailyCountPoint> ordersByDay = recentOrders
            .GroupBy(o => DateOnly.FromDateTime(o.CreatedAt))
            .Select(g => new DailyCountPoint
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(p => p.Date)
            .ToList();

        List<User> recentUsers = await _dbContext.Users
            .Where(u => u.CreatedAt >= thirtyDaysAgo)
            .ToListAsync(ct);

        List<DailyCountPoint> userRegistrationsByDay = recentUsers
            .GroupBy(u => DateOnly.FromDateTime(u.CreatedAt))
            .Select(g => new DailyCountPoint
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(p => p.Date)
            .ToList();

        int totalOrderCount = await _dbContext.Orders.CountAsync(ct);

        List<StatusBreakdownItem> ordersByStatus = await BreakdownAsync(_dbContext.Orders, o => o.Status, totalOrderCount, ct);
        List<StatusBreakdownItem> ordersByType = await BreakdownAsync(_dbContext.Orders, o => o.OrderType, totalOrderCount, ct);
        List<StatusBreakdownItem> paymentsByStatus = await BreakdownAsync(_dbContext.Orders, o => o.PaymentStatus, totalOrderCount, ct);

        List<TopRestaurantItem> topRestaurants = await _dbContext.Orders
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

        List<RecentOrderItem> latestOrders = await _dbContext.Orders
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

        decimal averageOrderValue = totalOrderCount > 0
            ? Math.Round(await _dbContext.Orders.AverageAsync(o => o.TotalAmount, ct), 2)
            : 0;

        List<Order> todayOrders = recentOrders.Where(o => o.CreatedAt.Date == today).ToList();
        int newUsersThisWeek = recentUsers.Count(u => u.CreatedAt >= weekAgo);

        AdminDashboardAnalyticsResponse analytics = new AdminDashboardAnalyticsResponse
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

    private static async Task<List<StatusBreakdownItem>> BreakdownAsync<TKey>(
        IQueryable<DeliverTableInfrastructure.Models.Order> orders,
        System.Linq.Expressions.Expression<Func<DeliverTableInfrastructure.Models.Order, TKey>> key,
        int total,
        CancellationToken ct) where TKey : notnull
    {
        var grouped = await orders
            .GroupBy(key)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return grouped
            .Select(g => new StatusBreakdownItem
            {
                Label = g.Key.ToString() ?? string.Empty,
                Count = g.Count,
                Percentage = total > 0 ? Math.Round((decimal)g.Count / total * 100, 1) : 0
            })
            .OrderByDescending(s => s.Count)
            .ToList();
    }
}
