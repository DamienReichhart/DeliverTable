using DeliverTableInfrastructure.Models;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface ILoyaltyRepository
{
    Task<LoyaltyProgram> CreateAsync(LoyaltyProgram program, CancellationToken ct = default);
    Task<LoyaltyProgram?> GetByRestaurantAsync(int restaurantId, CancellationToken ct = default);
    Task<LoyaltyProgram> UpdateAsync(LoyaltyProgram program, CancellationToken ct = default);
    Task<LoyaltyAccount?> GetAccountAsync(int programId, int customerId, CancellationToken ct = default);
    Task<LoyaltyAccount> CreateAccountAsync(LoyaltyAccount account, CancellationToken ct = default);
    Task<LoyaltyAccount> UpdateAccountAsync(LoyaltyAccount account, CancellationToken ct = default);
    Task<LoyaltyTransaction> CreateTransactionAsync(LoyaltyTransaction transaction, CancellationToken ct = default);

    // Admin methods
    Task<List<LoyaltyProgram>> GetAllProgramsUnscopedAsync(CancellationToken ct = default);
    Task<LoyaltyProgram?> GetProgramByIdWithAccountsAsync(int id, CancellationToken ct = default);
    Task<List<LoyaltyAccount>> GetAccountsByProgramIdAsync(int programId, CancellationToken ct = default);
    Task<List<LoyaltyTransaction>> GetTransactionsByAccountIdAsync(int accountId, CancellationToken ct = default);
    Task<bool> DeleteProgramAsync(int id, CancellationToken ct = default);
}
