using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableServer.Models;

public class ReclamationItem
{
    [ForeignKey("OrderItemId")]
    public int OrderItemId { get; set; }
    [ForeignKey("ReclamationId")]
    public int ReclamationId { get; set; }

    public bool HasAttachedImage { get; set; } = false;

    public Reclamation Reclamation { get; set; } = null!;
    public OrderItem OrderItem { get; set; } = null!;
}