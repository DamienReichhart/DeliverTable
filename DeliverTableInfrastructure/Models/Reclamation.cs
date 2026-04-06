using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Enums;
using DeliverTableSharedLibrary.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


[Table("Reclamation")]
public class Reclamation : ITrackable
{
    [Key]
    public int ReclamationId { get; set; }

    public int OrderId { get; set; }

    public ReclamationType Type { get; set; } = ReclamationType.Other;

    public ReclamationStatus Status { get; set; } = ReclamationStatus.Pending;

    public string Description { get; set; } = String.Empty;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Updated { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;

    public List<ReclamationItem> Items { get; set; } = [];

    public decimal? RefundAmount { get; set; } = null;
}