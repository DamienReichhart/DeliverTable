using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableInfrastructure.Models;

public class InvoiceLine
{
    [Key]
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public Invoice Invoice { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(9, 3)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal UnitPriceTtc { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal UnitPriceHt { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal VatRate { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal LineHt { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal LineVat { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal LineTtc { get; set; }

    public int SortOrder { get; set; }
}
