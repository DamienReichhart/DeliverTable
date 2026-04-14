using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Models;

public class Invoice
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Number { get; set; } = string.Empty;

    public InvoiceKind Kind { get; set; }

    public int OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;

    public InvoiceIssuerType IssuerType { get; set; }

    public int? IssuerRestaurantId { get; set; }

    [ForeignKey(nameof(IssuerRestaurantId))]
    public Restaurant? IssuerRestaurant { get; set; }

    public int? RecipientUserId { get; set; }

    [ForeignKey(nameof(RecipientUserId))]
    public User? RecipientUser { get; set; }

    public int? RecipientRestaurantId { get; set; }

    [ForeignKey(nameof(RecipientRestaurantId))]
    public Restaurant? RecipientRestaurant { get; set; }

    public int? RelatedInvoiceId { get; set; }

    [ForeignKey(nameof(RelatedInvoiceId))]
    public Invoice? RelatedInvoice { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(9, 2)")]
    public decimal TotalHt { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal TotalVat { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal TotalTtc { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    [MaxLength(400)]
    public string? StoragePath { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Queued;

    [MaxLength(2000)]
    public string? FailureReason { get; set; }

    [Required]
    [MaxLength(4000)]
    public string IssuerLegalSnapshotJson { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string RecipientSnapshotJson { get; set; } = string.Empty;

    public List<InvoiceLine> Lines { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
