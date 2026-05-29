namespace DeliverTableSharedLibrary.Dtos;

public sealed class CommissionStatementLineDto
{
    public int Id { get; set; }
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
