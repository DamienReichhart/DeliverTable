using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableServer.Models;

public class EventBookingPolicy
{
    [Key]
    public int Id { get; set; }

    public int EventId { get; set; }

    [ForeignKey("EventId")]
    public Event Event { get; set; } = null!;

    [Column(TypeName = "decimal(9, 2)")]
    public decimal? MinConfirmAmount { get; set; }

    [MaxLength(2000)]
    public string PolicySchema { get; set; } = string.Empty;
}
