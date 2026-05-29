using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableServer.Services.Interfaces;

public interface ICommissionStatementService
{
    Task<ServiceResult<CommissionStatementGenerationResultDto>> GenerateForPeriodAsync(
        int year, int month, CancellationToken ct);

    Task<ServiceResult> HandleRefundForPriorPeriodAsync(
        int orderId, int refundId, string stripeRefundId, decimal refundedAmount, CancellationToken ct);
}
