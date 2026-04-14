using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableServer.Common;

namespace DeliverTableServer.Services.Interfaces;

public interface IInvoiceService
{
    Task<ServiceResult<List<InvoiceJobMessage>>> CreatePendingInvoicesForCapturedOrderAsync(
        int orderId,
        CancellationToken ct);

    Task<ServiceResult<List<InvoiceJobMessage>>> CreateCreditNotesForRefundAsync(
        int refundId,
        CancellationToken ct);
}
