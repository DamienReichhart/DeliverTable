using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Extensions;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.Promotion;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class PromotionRepository(DeliverTableContext dbContext) : IPromotionRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<Promotion> CreateAsync(Promotion promotion, CancellationToken ct = default)
    {
        _dbContext.Promotions.Add(promotion);
        await _dbContext.SaveChangesAsync(ct);
        return promotion;
    }

    public async Task<Promotion?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Promotions
            .Include(p => p.PromotionDishes)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<(List<Promotion> Items, int TotalCount)> GetByRestaurantAsync(
        int restaurantId, PromotionQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.Promotions
            .Include(p => p.PromotionDishes)
            .Where(p => p.RestaurantId == restaurantId)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await q.CountAsync(ct);
        var (skip, take) = PaginationExtensions.GetPaginationOffsets(query.PageNumber, query.PageSize);
        var items = await q.Skip(skip).Take(take).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<List<Promotion>> GetActiveByRestaurantAsync(int restaurantId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _dbContext.Promotions
            .Include(p => p.PromotionDishes)
            .Where(p => p.RestaurantId == restaurantId && p.IsActive && p.StartsAt <= now && p.EndsAt >= now)
            .ToListAsync(ct);
    }

    public async Task<Promotion> UpdateAsync(Promotion promotion, CancellationToken ct = default)
    {
        promotion.UpdatedAt = DateTime.UtcNow;
        _dbContext.Promotions.Update(promotion);
        await _dbContext.SaveChangesAsync(ct);
        return promotion;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var promotion = await _dbContext.Promotions.FindAsync([id], ct);
        if (promotion is null) return false;
        _dbContext.Promotions.Remove(promotion);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<Promotion>> GetAllUnscopedAsync(CancellationToken ct = default)
    {
        return await _dbContext.Promotions
            .Include(p => p.Restaurant)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Promotion?> GetByIdWithRestaurantAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Promotions
            .Include(p => p.Restaurant)
            .Include(p => p.PromotionDishes)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }
}
