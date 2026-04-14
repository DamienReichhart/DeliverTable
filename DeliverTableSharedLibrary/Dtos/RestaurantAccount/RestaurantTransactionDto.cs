namespace DeliverTableSharedLibrary.Dtos.RestaurantAccount;

public class RestaurantTransactionDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime CreatedAt { get; set; }
}
