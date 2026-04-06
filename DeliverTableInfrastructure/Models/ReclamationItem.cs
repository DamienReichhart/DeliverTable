using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableServer.Models;

public class ReclamationItem
{
    [Key]
    public int Id { get; set; }

    public int OrderItemId { get; set; }

    public int ReclamationId { get; set; }

    public bool HasAttachedImage { get; set; } = false;

    [ForeignKey("ReclamationId")]
    public Reclamation Reclamation { get; set; } = null!;
    [ForeignKey("OrderItemId")]
    public OrderItem OrderItem { get; set; } = null!;
}