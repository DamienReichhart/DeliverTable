using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminLoyaltyTransactionResponse
{
    public int Id { get; set; }
    public LoyaltyTransactionType Type { get; set; }
    public int Points { get; set; }
    public int? OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}
