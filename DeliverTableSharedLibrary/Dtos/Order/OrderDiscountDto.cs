namespace DeliverTableSharedLibrary.Dtos.Order;

public class OrderDiscountDto
{
    public string Source { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
