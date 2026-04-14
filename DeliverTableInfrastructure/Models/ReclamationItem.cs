using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableServer.Models;

public class ReclamationItem
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("OrderItemId")]
    public int OrderItemId { get; set; }

    [ForeignKey("ReclamationId")]
    public int ReclamationId { get; set; }

    public bool HasAttachedImage { get; set; } = false;

    public Reclamation Reclamation { get; set; } = null!;
    public OrderItem OrderItem { get; set; } = null!;
}