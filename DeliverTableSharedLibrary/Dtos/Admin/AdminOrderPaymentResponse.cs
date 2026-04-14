namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminOrderPaymentResponse
{
    public int Id { get; set; }
    public string Provider { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
