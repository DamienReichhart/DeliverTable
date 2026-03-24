using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services.Interfaces;

public interface IAdminLoyaltyService
{
    Task<ServiceResult<List<AdminLoyaltyProgramResponse>>> GetAllProgramsAsync(CancellationToken ct = default);
    Task<ServiceResult<AdminLoyaltyProgramResponse>> GetProgramByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<AdminLoyaltyProgramResponse>> CreateProgramAsync(AdminCreateLoyaltyProgramRequest request, CancellationToken ct = default);
    Task<ServiceResult<AdminLoyaltyProgramResponse>> UpdateProgramAsync(int id, AdminUpdateLoyaltyProgramRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteProgramAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<List<AdminLoyaltyAccountResponse>>> GetAccountsAsync(int programId, CancellationToken ct = default);
    Task<ServiceResult<List<AdminLoyaltyTransactionResponse>>> GetTransactionsAsync(int accountId, CancellationToken ct = default);
}
