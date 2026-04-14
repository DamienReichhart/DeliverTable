namespace DeliverTableInfrastructure.TemplateData;

public sealed record InvoiceReadyCustomerData(
    string InvoiceNumber,
    string OrderId,
    string IssuedAt,
    string TotalTtc,
    string Currency);
