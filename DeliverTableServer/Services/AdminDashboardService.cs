using DeliverTableServer.Common;
using DeliverTableServer.Data;
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
}
