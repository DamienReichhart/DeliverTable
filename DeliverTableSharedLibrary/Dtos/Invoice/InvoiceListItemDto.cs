using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Invoice;

public record InvoiceListItemDto(
    int Id,
    string Number,
    InvoiceKind Kind,
    int OrderId,
    DateTime IssuedAt,
    decimal TotalTtc,
    string Currency,
    InvoiceStatus Status);
