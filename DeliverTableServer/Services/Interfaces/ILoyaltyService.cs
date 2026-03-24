using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Loyalty;

namespace DeliverTableServer.Services.Interfaces;

public interface ILoyaltyService
{
    Task<ServiceResult<LoyaltyProgramDto>> CreateOrUpdateProgramAsync(int restaurantId, int ownerId, CreateLoyaltyProgramRequest request, CancellationToken ct = default);
    Task<ServiceResult<LoyaltyProgramDto>> GetProgramAsync(int restaurantId, CancellationToken ct = default);
    Task<ServiceResult<LoyaltyAccountDto>> GetMyAccountAsync(int restaurantId, int customerId, CancellationToken ct = default);
}
