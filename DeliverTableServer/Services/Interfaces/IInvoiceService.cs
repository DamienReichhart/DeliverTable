using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableServer.Common;

namespace DeliverTableServer.Services.Interfaces;

public interface IInvoiceService
{
    Task<ServiceResult<List<InvoiceJobMessage>>> CreatePendingInvoicesForCapturedOrderAsync(
        int orderId,
        CancellationToken ct);
}
