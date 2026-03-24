using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Mappers;

public static class AdminRestaurantMapper
{
    public static AdminRestaurantResponse ToAdminDto(this Restaurant restaurant)
    {
        return new AdminRestaurantResponse
        {
            Id = restaurant.Id,
            Name = restaurant.Name,
            Type = restaurant.Type.ToString(),
            Description = restaurant.Description ?? "",
            AdressLine1 = restaurant.AdressLine1,
            AdressLine2 = restaurant.AdressLine2 ?? "",
            City = restaurant.City,
            ZipCode = restaurant.ZipCode,
            Country = restaurant.Country,
            Latitude = restaurant.Latitude,
            Longitude = restaurant.Longitude,
            IsActive = restaurant.IsActive,
            Balance = restaurant.Balance,
            OwnerId = restaurant.OwnerId,
            OwnerName = restaurant.Owner is not null
                ? $"{restaurant.Owner.FirstName} {restaurant.Owner.LastName}"
                : "",
            CreatedAt = restaurant.CreatedAt,
            UpdatedAt = restaurant.UpdatedAt
        };
    }
}
