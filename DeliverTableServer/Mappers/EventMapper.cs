using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Event;

namespace DeliverTableServer.Mappers;

public static class EventMapper
{
    public static RestaurantEventResponse ToRestaurantDto(this Event evt)
    {
        return new RestaurantEventResponse
        {
            Id = evt.Id,
            RestaurantId = evt.RestaurantId ?? 0,
            Name = evt.Name,
            Description = evt.Description,
            StartsAt = evt.StartsAt,
            EndsAt = evt.EndsAt,
            MaxGuests = evt.MaxGuests,
            IsActive = evt.IsActive,
            MenuItems = evt.EventMenuItems
                .Select(mi => mi.ToResponse())
                .ToList(),
            CreatedAt = evt.CreatedAt,
            UpdatedAt = evt.UpdatedAt
        };
    }

    private static EventMenuItemResponse ToResponse(this EventMenuItem item)
    {
        return new EventMenuItemResponse
        {
            Id = item.Id,
            DishId = item.DishId,
            Dish = item.Dish.ToDto(),
            OverridePrice = item.OverridePrice
        };
    }
}
