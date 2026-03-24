using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services.Interfaces;

public interface IAdminTransactionClientService
{
    Task<(List<AdminTransactionResponse>? Transactions, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default);

    Task<(AdminTransactionResponse? Transaction, ErrorResponse? Error)> GetByIdAsync(
        int id, CancellationToken ct = default);
}
