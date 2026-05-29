using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.CommissionStatement;

public sealed class AdminCommissionStatementRowDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public CommissionStatementKind Kind { get; set; }
    public int RecipientRestaurantId { get; set; }
    public string RecipientRestaurantName { get; set; } = string.Empty;
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public DateTime IssuedAt { get; set; }
    public decimal TotalTtc { get; set; }
    public CommissionStatementStatus Status { get; set; }
    public bool HasPdf { get; set; }
}
