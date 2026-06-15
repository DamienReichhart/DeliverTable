namespace DeliverTableSharedLibrary.Dtos.Event;

public class RestaurantEventResponse
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int? MaxGuests { get; set; }
    public bool IsActive { get; set; }
    public List<EventMenuItemResponse> MenuItems { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
