using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Models;

public class CommissionStatement
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(60)]
    public string Number { get; set; } = string.Empty;

    public CommissionStatementKind Kind { get; set; }

    public int RecipientRestaurantId { get; set; }
    [ForeignKey(nameof(RecipientRestaurantId))]
    public Restaurant RecipientRestaurant { get; set; } = null!;

    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }

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

    public CommissionStatementStatus Status { get; set; } = CommissionStatementStatus.Queued;

    [MaxLength(400)]
    public string? StoragePath { get; set; }

    [MaxLength(2000)]
    public string? FailureReason { get; set; }

    [Required]
    [MaxLength(4000)]
    public string IssuerLegalSnapshotJson { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string RecipientSnapshotJson { get; set; } = string.Empty;

    public int? RelatedStatementId { get; set; }
    [ForeignKey(nameof(RelatedStatementId))]
    public CommissionStatement? RelatedStatement { get; set; }

    [MaxLength(320)]
    public string? RecipientEmailSnapshot { get; set; }

    public List<CommissionStatementLine> Lines { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
