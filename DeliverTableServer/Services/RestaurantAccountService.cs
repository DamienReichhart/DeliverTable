using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Services;

public sealed class RestaurantAccountService(
    IRestaurantRepository restaurantRepository,
    IRestaurantTransactionRepository transactionRepository,
    AppEnvironment appEnvironment
) : IRestaurantAccountService
{
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;
    private readonly IRestaurantTransactionRepository _transactionRepository = transactionRepository;
    private readonly decimal _commissionRate = appEnvironment.PlatformCommissionRate;

    public async Task<ServiceResult<RestaurantAccountDto>> GetAccountAsync(
        int restaurantId, int ownerId, TransactionQuery query, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (restaurant.OwnerId != ownerId)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        var (items, totalCount) = await _transactionRepository.GetByRestaurantAsync(restaurantId, query, ct);

        return new RestaurantAccountDto
        {
            Balance = restaurant.Balance,
            Transactions = new PaginatedResult<RestaurantTransactionDto>
            {
                Items = items.Select(t => t.ToDto()).ToList(),
                TotalCount = totalCount,
                Page = query.PageNumber > 0 ? query.PageNumber : 1,
                PageSize = query.PageSize
            }
        };
    }

    public async Task<ServiceResult<RestaurantAccountDto>> WithdrawAsync(
        int restaurantId, int ownerId, WithdrawRequest request, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (restaurant.OwnerId != ownerId)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (request.Amount > restaurant.Balance)
            return new ServiceError(ErrorMessages.InsufficientBalance);

        restaurant.Balance -= request.Amount;
        await _restaurantRepository.UpdateAsync(restaurant, ct);

        var transaction = new Models.RestaurantTransaction
        {
            RestaurantId = restaurantId,
            OrderId = null,
            Type = TransactionType.Withdrawal,
            GrossAmount = request.Amount,
            CommissionAmount = 0,
            NetAmount = request.Amount,
            BalanceAfter = restaurant.Balance
        };

        await _transactionRepository.CreateAsync(transaction, ct);

        return await GetAccountAsync(restaurantId, ownerId, new TransactionQuery(), ct);
    }
}
