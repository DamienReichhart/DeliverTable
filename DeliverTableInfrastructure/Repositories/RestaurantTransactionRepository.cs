using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Extensions;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

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
        var items = await q.Paginate(query.PageNumber, query.PageSize).ToListAsync(ct);
        return (items, totalCount);
    }
}
