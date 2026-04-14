namespace DeliverTableSharedLibrary.Dtos.Invoice;

public record InvoiceLineDto(
    string Description,
    decimal Quantity,
    decimal UnitPriceHt,
    decimal UnitPriceTtc,
    decimal VatRate,
    decimal LineHt,
    decimal LineVat,
    decimal LineTtc);
