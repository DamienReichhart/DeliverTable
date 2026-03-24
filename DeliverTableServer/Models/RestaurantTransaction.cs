using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Models;

public class RestaurantTransaction
{
    [Key]
    public int Id { get; set; }

    public int RestaurantId { get; set; }

    [ForeignKey("RestaurantId")]
    public Restaurant Restaurant { get; set; } = null!;

    public int? OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    public TransactionType Type { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal GrossAmount { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal CommissionAmount { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal NetAmount { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal BalanceAfter { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
