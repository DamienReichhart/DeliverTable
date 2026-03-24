namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminTransactionResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal BalanceAfter { get; set; }
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = "";
    public int? OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}
