namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminRedemptionResponse
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public int OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}
