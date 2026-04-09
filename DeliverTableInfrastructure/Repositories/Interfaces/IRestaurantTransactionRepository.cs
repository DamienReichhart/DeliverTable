using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface IRestaurantTransactionRepository
{
    Task<RestaurantTransaction> CreateAsync(RestaurantTransaction transaction, CancellationToken ct = default);
    Task<(List<RestaurantTransaction> Items, int TotalCount)> GetByRestaurantAsync(int restaurantId, TransactionQuery query, CancellationToken ct = default);
}
