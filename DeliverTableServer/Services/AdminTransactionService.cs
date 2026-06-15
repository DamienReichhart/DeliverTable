using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableInfrastructure.Models;

namespace DeliverTableServer.Services;

public sealed class AdminTransactionService(ITransactionRepository transactionRepository) : IAdminTransactionService
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;

    public async Task<ServiceResult<List<AdminTransactionResponse>>> GetAllAsync(CancellationToken ct = default)
    {
        List<RestaurantTransaction> transactions = await _transactionRepository.GetAllAsync(ct);
        List<AdminTransactionResponse> result = transactions.Select(t => t.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminTransactionResponse>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        RestaurantTransaction? transaction = await _transactionRepository.GetByIdAsync(id, ct);
        if (transaction is null)
            return ServiceError.NotFound(ErrorMessages.TransactionNotFound);

        return transaction.ToAdminDto();
    }
}
