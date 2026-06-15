using DeliverTableSharedLibrary.Dtos.Dish;

namespace DeliverTableSharedLibrary.Dtos.Event;

public class EventMenuItemResponse
{
    public int Id { get; set; }

    public int DishId { get; set; }

    public DishDto Dish { get; set; } = new();

    public decimal? OverridePrice { get; set; }
}
