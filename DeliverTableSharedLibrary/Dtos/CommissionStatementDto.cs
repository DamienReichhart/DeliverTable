using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos;

public sealed class CommissionStatementDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public CommissionStatementKind Kind { get; set; }
    public int RecipientRestaurantId { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public DateTime IssuedAt { get; set; }
    public decimal TotalHt { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalTtc { get; set; }
    public string Currency { get; set; } = "EUR";
    public CommissionStatementStatus Status { get; set; }
    public int? RelatedStatementId { get; set; }
    public List<CommissionStatementLineDto> Lines { get; set; } = [];
}
