using System.ComponentModel.DataAnnotations;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminCreateModerationActionRequest
{
    [Required(ErrorMessage = "Le type de cible est obligatoire")]
    public ModerationTargetType TargetType { get; set; }

    [Required(ErrorMessage = "L'identifiant de la cible est obligatoire")]
    public int TargetId { get; set; }

    [Required(ErrorMessage = "Le type d'action est obligatoire")]
    public ModerationActionType ActionType { get; set; }

    [MaxLength(2000)]
    public string? Reason { get; set; }
}
