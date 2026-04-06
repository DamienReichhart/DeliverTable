using DeliverTableSharedLibrary.Enums;
using DeliverTableSharedLibrary.Dtos.Order;

namespace DeliverTableSharedLibrary.Dtos.Reclamation;

public class ReclamationDto
{
    public int ReclamationId { get; set; }
    public List<ReclamationItemDto> Items { get; set; } = [];
    public ReclamationType Type { get; set; } = ReclamationType.Other;
    public ReclamationStatus Status { get; set; } = ReclamationStatus.Pending;
    public string Description { get; set; } = String.Empty;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
    public int OrderId { get; set; }
    public decimal OrderTotalAmount { get; set; }
    public decimal? RefundAmount { get; set; } = null;
}
