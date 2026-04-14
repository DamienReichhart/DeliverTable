namespace DeliverTableInfrastructure.TemplateData;

public sealed record InvoiceReadyRestaurantData(
    string InvoiceNumber,
    string OrderId,
    string IssuedAt,
    string TotalTtc,
    string Currency);
