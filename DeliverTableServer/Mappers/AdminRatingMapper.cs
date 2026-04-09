using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Mappers;

public static class AdminRatingMapper
{
    public static AdminRestaurantRatingResponse ToAdminDto(this RestaurantRating rating)
    {
        return new AdminRestaurantRatingResponse
        {
            Id = rating.Id,
            Rating = rating.Rating,
            Comment = rating.Comment,
            RestaurantName = rating.Restaurant is not null
                ? rating.Restaurant.Name
                : "",
            CustomerName = rating.CustomerUser is not null
                ? $"{rating.CustomerUser.FirstName} {rating.CustomerUser.LastName}"
                : "",
            OrderId = rating.OrderId,
            CreatedAt = rating.CreatedAt
        };
    }
}
