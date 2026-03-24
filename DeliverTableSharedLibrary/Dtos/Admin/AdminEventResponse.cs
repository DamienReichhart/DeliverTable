using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminEventResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int? MaxGuests { get; set; }
    public EventVisibility Visibility { get; set; }
    public bool IsActive { get; set; }
    public int? RestaurantId { get; set; }
    public string RestaurantName { get; set; } = "";
    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
