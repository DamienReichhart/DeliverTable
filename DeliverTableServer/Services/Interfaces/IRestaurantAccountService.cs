using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;

namespace DeliverTableServer.Services.Interfaces;

public interface IRestaurantAccountService
{
    Task<ServiceResult<RestaurantAccountDto>> GetAccountAsync(int restaurantId, int ownerId, TransactionQuery query, CancellationToken ct = default);
    Task<ServiceResult<RestaurantAccountDto>> WithdrawAsync(int restaurantId, int ownerId, WithdrawRequest request, CancellationToken ct = default);
}
