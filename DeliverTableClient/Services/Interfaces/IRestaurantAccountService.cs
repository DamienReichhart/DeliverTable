using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;

namespace DeliverTableClient.Services.Interfaces;

public interface IRestaurantAccountService
{
    Task<(RestaurantAccountDto?, ErrorResponse?)> GetAccountAsync(
        int restaurantId, TransactionQuery query, CancellationToken ct = default);
    Task<(RestaurantAccountDto?, ErrorResponse?)> WithdrawAsync(
        int restaurantId, WithdrawRequest request, CancellationToken ct = default);
}
