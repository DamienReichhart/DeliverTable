using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Reclamation;

public class UpdateReclamationDto
{
    public ReclamationType Type { get; set; } = ReclamationType.Other;
    public ReclamationStatus Status { get; set; } = ReclamationStatus.Pending;
    public string Description { get; set; } = string.Empty;
}
