using DeliverTableServer.Data;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories;

public class RestaurantTransactionRepository(DeliverTableContext dbContext) : IRestaurantTransactionRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<RestaurantTransaction> CreateAsync(RestaurantTransaction transaction, CancellationToken ct = default)
    {
        _dbContext.RestaurantTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync(ct);
        return transaction;
    }

    public async Task<(List<RestaurantTransaction> Items, int TotalCount)> GetByRestaurantAsync(
        int restaurantId, TransactionQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.RestaurantTransactions
            .Where(t => t.RestaurantId == restaurantId)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await q.CountAsync(ct);

        int page = query.PageNumber > 0 ? query.PageNumber : 1;
        int skip = (page - 1) * query.PageSize;

        var items = await q.Skip(skip).Take(query.PageSize).ToListAsync(ct);
        return (items, totalCount);
    }
}
