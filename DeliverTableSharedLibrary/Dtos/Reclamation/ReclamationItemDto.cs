using DeliverTableSharedLibrary.Dtos.Order;

namespace DeliverTableSharedLibrary.Dtos.Reclamation;

public class ReclamationItemDto
{
    public int Id { get; set; }
    public OrderItemDto Item { get; set; } = null!;
    public bool HasAttachedImage { get; set; } = false;
    public string? ImagePath { get; set; } = null;
}
