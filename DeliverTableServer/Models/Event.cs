using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Models;

public class Event
{
    [Key]
    public int Id { get; set; }

    public int? RestaurantId { get; set; }

    [ForeignKey("RestaurantId")]
    public Restaurant? Restaurant { get; set; }

    public int CreatedByUserId { get; set; }

    [ForeignKey("CreatedByUserId")]
    public User CreatedByUser { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public int? MaxGuests { get; set; }

    public EventVisibility Visibility { get; set; } = EventVisibility.Public;

    public bool IsActive { get; set; } = true;

    public List<EventMenuItem> EventMenuItems { get; set; } = [];

    public List<EventBookingPolicy> EventBookingPolicies { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
