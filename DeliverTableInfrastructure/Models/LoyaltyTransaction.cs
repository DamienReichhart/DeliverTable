using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Models;

public class LoyaltyTransaction
{
    [Key]
    public int Id { get; set; }

    public int LoyaltyAccountId { get; set; }

    [ForeignKey("LoyaltyAccountId")]
    public LoyaltyAccount LoyaltyAccount { get; set; } = null!;

    public LoyaltyTransactionType Type { get; set; }

    public int Points { get; set; }

    public int? OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
