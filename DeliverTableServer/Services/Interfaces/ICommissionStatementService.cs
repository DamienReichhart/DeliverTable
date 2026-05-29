using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.CommissionStatement;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Services.Interfaces;

public interface ICommissionStatementService
{
    Task<ServiceResult<CommissionStatementGenerationResultDto>> GenerateForPeriodAsync(
        int year, int month, CancellationToken ct);

    Task<ServiceResult> HandleRefundForPriorPeriodAsync(
        int orderId, int refundId, string stripeRefundId, decimal refundedAmount, CancellationToken ct);

    Task<ServiceResult<PaginatedResult<AdminCommissionStatementRowDto>>> AdminListAsync(
        int? year, CommissionStatementKind? kind, int? restaurantId, int page, int pageSize, CancellationToken ct);

    Task<ServiceResult<AdminCommissionStatementDetailDto>> AdminGetDetailAsync(int id, CancellationToken ct);

    Task<ServiceResult<(byte[] Pdf, string FileName)>> AdminGetPdfAsync(int id, CancellationToken ct);
}
