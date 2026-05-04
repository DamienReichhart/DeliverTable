using DeliverTableInfrastructure.Extensions;
using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Mappers;

public static class AdminEventMapper
{
    public static AdminEventResponse ToAdminDto(this Event evt)
    {
        return new AdminEventResponse
        {
            Id = evt.Id,
            Name = evt.Name,
            Description = evt.Description,
            StartsAt = evt.StartsAt,
            EndsAt = evt.EndsAt,
            MaxGuests = evt.MaxGuests,
            Visibility = evt.Visibility,
            IsActive = evt.IsActive,
            RestaurantId = evt.RestaurantId,
            RestaurantName = evt.Restaurant is not null
                ? evt.Restaurant.Name
                : "",
            CreatedByUserId = evt.CreatedByUserId,
            CreatedByUserName = evt.CreatedByUser?.GetFullName() ?? "",
            CreatedAt = evt.CreatedAt,
            UpdatedAt = evt.UpdatedAt
        };
    }
}
