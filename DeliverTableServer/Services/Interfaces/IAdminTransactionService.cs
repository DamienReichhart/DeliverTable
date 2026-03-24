using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services.Interfaces;

public interface IAdminTransactionService
{
    Task<ServiceResult<List<AdminTransactionResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResult<AdminTransactionResponse>> GetByIdAsync(int id, CancellationToken ct = default);
}
