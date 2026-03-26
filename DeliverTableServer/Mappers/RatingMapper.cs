using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.Rating;

namespace DeliverTableServer.Mappers;

public static class RatingMapper
{
    public static RatingDto ToDto(this RestaurantRating rating)
    {
        return new RatingDto
        {
            Id = rating.Id,
            OrderId = rating.OrderId,
            RestaurantId = rating.RestaurantId,
            RestaurantName = rating.Restaurant is not null
                ? rating.Restaurant.Name
                : string.Empty,
            Rating = rating.Rating,
            Comment = rating.Comment,
            CreatedAt = rating.CreatedAt
        };
    }
}
