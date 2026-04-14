namespace DeliverTableSharedLibrary.Dtos.Invoice;

public record AdminInvoiceDetailDto(
    InvoiceListItemDto Header,
    List<InvoiceLineDto> Lines,
    InvoiceLegalSnapshotDto Issuer,
    InvoiceLegalSnapshotDto Recipient,
    int? RelatedInvoiceId);
