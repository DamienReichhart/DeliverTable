using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

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

    public async Task<List<LoyaltyProgram>> GetAllProgramsUnscopedAsync(CancellationToken ct = default)
    {
        return await _dbContext.LoyaltyPrograms
            .Include(lp => lp.Restaurant)
            .Include(lp => lp.Accounts)
            .OrderByDescending(lp => lp.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<LoyaltyProgram?> GetProgramByIdWithAccountsAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.LoyaltyPrograms
            .Include(lp => lp.Restaurant)
            .Include(lp => lp.Accounts)
            .FirstOrDefaultAsync(lp => lp.Id == id, ct);
    }

    public async Task<List<LoyaltyAccount>> GetAccountsByProgramIdAsync(int programId, CancellationToken ct = default)
    {
        return await _dbContext.LoyaltyAccounts
            .Include(la => la.Customer)
            .Where(la => la.LoyaltyProgramId == programId)
            .OrderByDescending(la => la.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<LoyaltyTransaction>> GetTransactionsByAccountIdAsync(int accountId, CancellationToken ct = default)
    {
        return await _dbContext.LoyaltyTransactions
            .Where(lt => lt.LoyaltyAccountId == accountId)
            .OrderByDescending(lt => lt.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteProgramAsync(int id, CancellationToken ct = default)
    {
        LoyaltyProgram? program = await _dbContext.LoyaltyPrograms.FindAsync([id], ct);
        if (program is null) return false;
        _dbContext.LoyaltyPrograms.Remove(program);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task MarkPendingRedemptionsCommittedForOrderAsync(int orderId, CancellationToken ct = default)
    {
        List<LoyaltyTransaction> rows = await _dbContext.LoyaltyTransactions
            .Where(lt => lt.OrderId == orderId && lt.Status == LoyaltyRedemptionStatus.Pending)
            .ToListAsync(ct);

        foreach (LoyaltyTransaction? row in rows)
        {
            row.Status = LoyaltyRedemptionStatus.Committed;
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task MarkPendingRedemptionsReversedForOrderAsync(int orderId, CancellationToken ct = default)
    {
        List<LoyaltyTransaction> rows = await _dbContext.LoyaltyTransactions
            .Include(lt => lt.LoyaltyAccount)
            .Where(lt => lt.OrderId == orderId && lt.Status == LoyaltyRedemptionStatus.Pending)
            .ToListAsync(ct);

        foreach (LoyaltyTransaction? row in rows)
        {
            row.Status = LoyaltyRedemptionStatus.Reversed;

            // Reversal subtracts the stored Points value.
            // Redeem rows store Points as negative (e.g. -30), so subtracting restores +30.
            // Earn rows store Points as positive (e.g. +10), so subtracting removes -10.
            row.LoyaltyAccount.PointsBalance -= row.Points;
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}
