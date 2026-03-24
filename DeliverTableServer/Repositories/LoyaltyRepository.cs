using DeliverTableServer.Data;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories;

public class LoyaltyRepository(DeliverTableContext dbContext) : ILoyaltyRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<LoyaltyProgram> CreateAsync(LoyaltyProgram program, CancellationToken ct = default)
    {
        _dbContext.LoyaltyPrograms.Add(program);
        await _dbContext.SaveChangesAsync(ct);
        return program;
    }

    public async Task<LoyaltyProgram?> GetByRestaurantAsync(int restaurantId, CancellationToken ct = default)
    {
        return await _dbContext.LoyaltyPrograms
            .FirstOrDefaultAsync(lp => lp.RestaurantId == restaurantId, ct);
    }

    public async Task<LoyaltyProgram> UpdateAsync(LoyaltyProgram program, CancellationToken ct = default)
    {
        program.UpdatedAt = DateTime.UtcNow;
        _dbContext.LoyaltyPrograms.Update(program);
        await _dbContext.SaveChangesAsync(ct);
        return program;
    }

    public async Task<LoyaltyAccount?> GetAccountAsync(int programId, int customerId, CancellationToken ct = default)
    {
        return await _dbContext.LoyaltyAccounts
            .FirstOrDefaultAsync(la => la.LoyaltyProgramId == programId && la.CustomerId == customerId, ct);
    }

    public async Task<LoyaltyAccount> CreateAccountAsync(LoyaltyAccount account, CancellationToken ct = default)
    {
        _dbContext.LoyaltyAccounts.Add(account);
        await _dbContext.SaveChangesAsync(ct);
        return account;
    }

    public async Task<LoyaltyAccount> UpdateAccountAsync(LoyaltyAccount account, CancellationToken ct = default)
    {
        account.UpdatedAt = DateTime.UtcNow;
        _dbContext.LoyaltyAccounts.Update(account);
        await _dbContext.SaveChangesAsync(ct);
        return account;
    }

    public async Task<LoyaltyTransaction> CreateTransactionAsync(LoyaltyTransaction transaction, CancellationToken ct = default)
    {
        _dbContext.LoyaltyTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync(ct);
        return transaction;
    }
}
