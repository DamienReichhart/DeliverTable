using DeliverTableServer.Data;
using DeliverTableServer.Extensions;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.DiscountCode;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories;

public class DiscountCodeRepository(DeliverTableContext dbContext) : IDiscountCodeRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<Models.DiscountCode> CreateAsync(Models.DiscountCode code, CancellationToken ct = default)
    {
        _dbContext.DiscountCodes.Add(code);
        await _dbContext.SaveChangesAsync(ct);
        return code;
    }

    public async Task<Models.DiscountCode?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.DiscountCodes.FirstOrDefaultAsync(dc => dc.Id == id, ct);
    }

    public async Task<Models.DiscountCode?> GetByCodeAndRestaurantAsync(string code, int restaurantId, CancellationToken ct = default)
    {
        return await _dbContext.DiscountCodes
            .FirstOrDefaultAsync(dc => dc.Code == code && dc.RestaurantId == restaurantId, ct);
    }

    public async Task<(List<Models.DiscountCode> Items, int TotalCount)> GetByRestaurantAsync(
        int restaurantId, DiscountCodeQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.DiscountCodes
            .Where(dc => dc.RestaurantId == restaurantId)
            .OrderByDescending(dc => dc.CreatedAt);

        var totalCount = await q.CountAsync(ct);
        var (skip, take) = PaginationExtensions.GetPaginationOffsets(query.PageNumber, query.PageSize);
        var items = await q.Skip(skip).Take(take).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<int> GetRedemptionCountByUserAsync(int discountCodeId, int customerId, CancellationToken ct = default)
    {
        return await _dbContext.DiscountCodeRedemptions
            .CountAsync(r => r.DiscountCodeId == discountCodeId && r.CustomerId == customerId, ct);
    }

    public async Task<Models.DiscountCode> UpdateAsync(Models.DiscountCode code, CancellationToken ct = default)
    {
        code.UpdatedAt = DateTime.UtcNow;
        _dbContext.DiscountCodes.Update(code);
        await _dbContext.SaveChangesAsync(ct);
        return code;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var code = await _dbContext.DiscountCodes.FindAsync([id], ct);
        if (code is null) return false;
        _dbContext.DiscountCodes.Remove(code);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<DiscountCodeRedemption> CreateRedemptionAsync(DiscountCodeRedemption redemption, CancellationToken ct = default)
    {
        _dbContext.DiscountCodeRedemptions.Add(redemption);
        await _dbContext.SaveChangesAsync(ct);
        return redemption;
    }

    public async Task<List<Models.DiscountCode>> GetAllUnscopedAsync(CancellationToken ct = default)
    {
        return await _dbContext.DiscountCodes
            .Include(dc => dc.Restaurant)
            .Include(dc => dc.Redemptions)
            .OrderByDescending(dc => dc.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Models.DiscountCode?> GetByIdWithRestaurantAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.DiscountCodes
            .Include(dc => dc.Restaurant)
            .Include(dc => dc.Redemptions)
            .FirstOrDefaultAsync(dc => dc.Id == id, ct);
    }

    public async Task<List<DiscountCodeRedemption>> GetRedemptionsByCodeIdAsync(int discountCodeId, CancellationToken ct = default)
    {
        return await _dbContext.DiscountCodeRedemptions
            .Include(r => r.Customer)
            .Where(r => r.DiscountCodeId == discountCodeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }
}
