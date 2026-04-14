using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Invoice;

public record AdminInvoiceRowDto(
    int Id,
    string Number,
    InvoiceKind Kind,
    InvoiceIssuerType IssuerType,
    string IssuerName,
    string RecipientName,
    DateTime IssuedAt,
    decimal TotalTtc,
    InvoiceStatus Status);
