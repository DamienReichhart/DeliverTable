using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Mappers;

public static class AdminPromotionMapper
{
    public static AdminPromotionResponse ToAdminDto(this Promotion promotion)
    {
        return new AdminPromotionResponse
        {
            Id = promotion.Id,
            Name = promotion.Name,
            Description = promotion.Description,
            PromotionType = promotion.PromotionType,
            DiscountType = promotion.DiscountType,
            DiscountValue = promotion.DiscountValue,
            MinOrderAmount = promotion.MinOrderAmount,
            StartsAt = promotion.StartsAt,
            EndsAt = promotion.EndsAt,
            IsActive = promotion.IsActive,
            RestaurantId = promotion.RestaurantId,
            RestaurantName = promotion.Restaurant is not null
                ? promotion.Restaurant.Name
                : "",
            CreatedAt = promotion.CreatedAt,
            UpdatedAt = promotion.UpdatedAt
        };
    }
}
