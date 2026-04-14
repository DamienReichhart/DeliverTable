using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Invoice;

public class InvoiceAdminQuery
{
    public int? Year { get; set; }
    public InvoiceKind? Kind { get; set; }
    public InvoiceIssuerType? IssuerType { get; set; }
    public int? RestaurantId { get; set; }
    public string? CustomerEmail { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
