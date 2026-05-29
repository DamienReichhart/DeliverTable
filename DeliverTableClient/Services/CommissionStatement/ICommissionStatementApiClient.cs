using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.CommissionStatement;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableClient.Services.CommissionStatement;

public interface ICommissionStatementApiClient
{
    Task<CommissionStatementGenerationResultDto?> RunAsync(int? year, int? month);

    Task<PaginatedResult<AdminCommissionStatementRowDto>?> AdminListAsync(
        int? year, CommissionStatementKind? kind, int? restaurantId, int page, int pageSize);

    Task<AdminCommissionStatementDetailDto?> AdminGetAsync(int id);

    Task DownloadPdfAsync(int id);
}
