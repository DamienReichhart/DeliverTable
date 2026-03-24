using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services.Interfaces;

public interface IAdminLoyaltyClientService
{
    Task<(List<AdminLoyaltyProgramResponse>? Programs, ErrorResponse? Error)> GetAllProgramsAsync(
        CancellationToken ct = default);

    Task<(AdminLoyaltyProgramResponse? Program, ErrorResponse? Error)> GetProgramByIdAsync(
        int id, CancellationToken ct = default);

    Task<(AdminLoyaltyProgramResponse? Program, ErrorResponse? Error)> CreateProgramAsync(
        AdminCreateLoyaltyProgramRequest request, CancellationToken ct = default);

    Task<(AdminLoyaltyProgramResponse? Program, ErrorResponse? Error)> UpdateProgramAsync(
        int id, AdminUpdateLoyaltyProgramRequest request, CancellationToken ct = default);

    Task<(bool Success, ErrorResponse? Error)> DeleteProgramAsync(int id, CancellationToken ct = default);

    Task<(List<AdminLoyaltyAccountResponse>? Accounts, ErrorResponse? Error)> GetAccountsAsync(
        int programId, CancellationToken ct = default);

    Task<(List<AdminLoyaltyTransactionResponse>? Transactions, ErrorResponse? Error)> GetTransactionsAsync(
        int programId, int accountId, CancellationToken ct = default);
}
