using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Models;

public class ModerationAction
{
    [Key]
    public int Id { get; set; }

    public int AdminUserId { get; set; }

    [ForeignKey("AdminUserId")]
    public User AdminUser { get; set; } = null!;

    public ModerationTargetType TargetType { get; set; }

    public int TargetId { get; set; }

    public ModerationActionType ActionType { get; set; }

    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
