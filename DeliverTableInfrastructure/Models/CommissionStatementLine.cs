using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableInfrastructure.Models;

public class CommissionStatementLine
{
    [Key]
    public int Id { get; set; }

    public int CommissionStatementId { get; set; }
    [ForeignKey(nameof(CommissionStatementId))]
    public CommissionStatement CommissionStatement { get; set; } = null!;

    public int OrderId { get; set; }
    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    public DateTime OrderCompletedAt { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal OrderTotalAmount { get; set; }

    [Column(TypeName = "decimal(5, 4)")]
    public decimal CommissionRateSnapshot { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal VatRate { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal LineHt { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal LineVat { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal LineTtc { get; set; }

    [MaxLength(100)]
    public string? RefundEventId { get; set; }

    public int SortOrder { get; set; }
}
