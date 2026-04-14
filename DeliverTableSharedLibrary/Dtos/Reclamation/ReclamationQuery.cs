namespace DeliverTableSharedLibrary.Dtos.Reclamation;

public class ReclamationQuery
{
    public string? ReclamationType { get; set; } = null;
    public string? ReclamationStatus { get; set; } = null;
    public string? Content { get; set; } = null;
    public uint PageNumber { get; set; } = 1;
    public uint PageSize { get; set; } = 100;
}
