using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Invoicing;

public interface IInvoiceNumberingService
{
    Task<string> IssueNumberAsync(
        InvoiceIssuerType issuerType,
        int? issuerEntityId,
        int year,
        bool isCreditNote,
        CancellationToken ct);
}
