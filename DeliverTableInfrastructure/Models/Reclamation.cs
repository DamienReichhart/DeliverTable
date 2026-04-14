using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;
using DeliverTableSharedLibrary.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Models;

[Table("Reclamation")]
[PrimaryKey(nameof(ReclamationId))]
public class Reclamation : ITrackable
{
    public int ReclamationId { get; set; }

    [ForeignKey("OrderId")]
    public int OrderId { get; set; }

    public ReclamationType Type { get; set; } = ReclamationType.Other;

    public ReclamationStatus Status { get; set; } = ReclamationStatus.Pending;

    public string Description { get; set; } = String.Empty;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Updated { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
}