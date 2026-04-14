namespace DeliverTableSharedLibrary.Dtos.Invoice;

public record InvoiceLegalSnapshotDto(
    string Name,
    string LegalForm,
    string Siret,
    string VatNumber,
    string Address,
    string Email = "");
