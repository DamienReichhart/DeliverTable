using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Extensions;
using DeliverTableServer.Extensions;
using DeliverTableServer.Helpers;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
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
        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, restaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;
        var restaurant = ownershipResult.Value!;

        var data = await _transactionRepository.GetByRestaurantAsync(restaurantId, query, ct);

        return new RestaurantAccountDto
        {
            Balance = restaurant.Balance,
            Transactions = data.ToPaginatedResult(t => t.ToDto(), query.PageNumber, query.PageSize)
        };
    }

    public async Task<ServiceResult<RestaurantAccountDto>> WithdrawAsync(
        int restaurantId, int ownerId, WithdrawRequest request, CancellationToken ct = default)
    {
        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, restaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;
        var restaurant = ownershipResult.Value!;

        if (request.Amount > restaurant.Balance)
            return new ServiceError(ErrorMessages.InsufficientBalance);

        restaurant.Balance -= request.Amount;
        await _restaurantRepository.UpdateAsync(restaurant, ct);

        var transaction = new RestaurantTransaction
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
