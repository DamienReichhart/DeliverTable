using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services;

public sealed class AdminTransactionService(ITransactionRepository transactionRepository) : IAdminTransactionService
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;

    public async Task<ServiceResult<List<AdminTransactionResponse>>> GetAllAsync(CancellationToken ct = default)
    {
        var transactions = await _transactionRepository.GetAllAsync(ct);
        var result = transactions.Select(t => t.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminTransactionResponse>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id, ct);
        if (transaction is null)
            return new ServiceError(ErrorMessages.TransactionNotFound, 404);

        return transaction.ToAdminDto();
    }
}
