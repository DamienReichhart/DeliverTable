using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableServer.Models;

public class OrderBlockedSlot
{
    [Key]
    public int Id { get; set; }

    public int RestaurantId { get; set; }

    [ForeignKey("RestaurantId")]
    public Restaurant Restaurant { get; set; } = null!;

    public int? RestaurantTableId { get; set; }

    [ForeignKey("RestaurantTableId")]
    public RestaurantTable? RestaurantTable { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
