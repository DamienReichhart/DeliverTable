using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Loyalty;

namespace DeliverTableClient.Services.Interfaces;

public interface ILoyaltyClientService
{
    Task<(LoyaltyProgramDto?, ErrorResponse?)> GetProgramAsync(int restaurantId, CancellationToken ct = default);
    Task<(LoyaltyProgramDto?, ErrorResponse?)> CreateOrUpdateAsync(int restaurantId, CreateLoyaltyProgramRequest request, CancellationToken ct = default);
    Task<(LoyaltyAccountDto?, ErrorResponse?)> GetMyAccountAsync(int restaurantId, CancellationToken ct = default);
}
