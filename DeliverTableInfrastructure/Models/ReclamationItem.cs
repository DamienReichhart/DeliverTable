using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableServer.Models;

public class ReclamationItem
{
    [ForeignKey("OrderId")]
    public int OrderId { get; set; }
    [ForeignKey("ReclamationId")]
    public int ReclamationId { get; set; }
    // Ajouter l'item qui pose problème

    public Reclamation Reclamation { get; set; } = null!;
    public Order Order { get; set; } = null!;
}