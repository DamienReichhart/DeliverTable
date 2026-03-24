using DeliverTableServer.Data;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories;

public class TransactionRepository(DeliverTableContext dbContext) : ITransactionRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<List<RestaurantTransaction>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbContext.RestaurantTransactions
            .Include(t => t.Restaurant)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<RestaurantTransaction?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.RestaurantTransactions
            .Include(t => t.Restaurant)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }
}
