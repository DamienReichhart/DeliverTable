using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Restaurant;

namespace DeliverTableServer.Mappers;

public static class RestaurantMappers
{
    public static RestaurantDto ToDto(this Restaurant restaurantModel)
    {
        return new RestaurantDto
        {
            Id = restaurantModel.Id,
            Name = restaurantModel.Name,
            Type = restaurantModel.Type.ToString(),
            City = restaurantModel.City,
            ZipCode = restaurantModel.ZipCode,
            Country = restaurantModel.Country,
        };
    }

    public static RestaurantMapDto ToMapDto(this Restaurant restaurantModel)
    {
        return new RestaurantMapDto(
            restaurantModel.Id,
            restaurantModel.Name,
            restaurantModel.Type.ToString(),
            restaurantModel.Latitude,
            restaurantModel.Longitude
        );
    }

    public static DetailedRestaurantDto ToDetailedDto(this Restaurant restaurantModel)
    {
        return new DetailedRestaurantDto
        {
            Id = restaurantModel.Id,
            Name = restaurantModel.Name,
            Type = restaurantModel.Type.ToString(),
            Description = restaurantModel.Description,
            AdressLine1 = restaurantModel.AdressLine1,
            AdressLine2 = restaurantModel.AdressLine2,
            City = restaurantModel.City,
            ZipCode = restaurantModel.ZipCode,
            Country = restaurantModel.Country,
            Latitude = restaurantModel.Latitude,
            Longitude = restaurantModel.Longitude,
            IsActive = restaurantModel.IsActive
        };
    }
}
