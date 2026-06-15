namespace DeliverTableSharedLibrary.Dtos.Event;

public class EventMenuItemRequest
{
    public int DishId { get; set; }

    public decimal? OverridePrice { get; set; }
}
