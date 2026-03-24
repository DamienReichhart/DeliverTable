using DeliverTableServer.Models;

namespace DeliverTableServer.Repositories.Interfaces;

public interface ILoyaltyRepository
{
    Task<LoyaltyProgram> CreateAsync(LoyaltyProgram program, CancellationToken ct = default);
    Task<LoyaltyProgram?> GetByRestaurantAsync(int restaurantId, CancellationToken ct = default);
    Task<LoyaltyProgram> UpdateAsync(LoyaltyProgram program, CancellationToken ct = default);
    Task<LoyaltyAccount?> GetAccountAsync(int programId, int customerId, CancellationToken ct = default);
    Task<LoyaltyAccount> CreateAccountAsync(LoyaltyAccount account, CancellationToken ct = default);
    Task<LoyaltyAccount> UpdateAccountAsync(LoyaltyAccount account, CancellationToken ct = default);
    Task<LoyaltyTransaction> CreateTransactionAsync(LoyaltyTransaction transaction, CancellationToken ct = default);
}
