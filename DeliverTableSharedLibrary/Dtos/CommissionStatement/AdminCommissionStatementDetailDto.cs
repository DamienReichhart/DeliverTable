using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.CommissionStatement;

public sealed class AdminCommissionStatementDetailDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public CommissionStatementKind Kind { get; set; }
    public int RecipientRestaurantId { get; set; }
    public string RecipientRestaurantName { get; set; } = string.Empty;
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public DateTime IssuedAt { get; set; }
    public decimal TotalHt { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalTtc { get; set; }
    public CommissionStatementStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public int? RelatedStatementId { get; set; }
    public List<AdminCommissionStatementLineDto> Lines { get; set; } = [];
}

public sealed class AdminCommissionStatementLineDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderCompletedAt { get; set; }
    public decimal OrderTotalAmount { get; set; }
    public decimal CommissionRateSnapshot { get; set; }
    public decimal VatRate { get; set; }
    public decimal LineHt { get; set; }
    public decimal LineVat { get; set; }
    public decimal LineTtc { get; set; }
    public string? RefundEventId { get; set; }
}
