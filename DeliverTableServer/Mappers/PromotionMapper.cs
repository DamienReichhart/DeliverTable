using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Promotion;

namespace DeliverTableServer.Mappers;

public static class PromotionMapper
{
    public static PromotionDto ToDto(this Promotion promotion)
    {
        return new PromotionDto
        {
            Id = promotion.Id,
            RestaurantId = promotion.RestaurantId,
            Name = promotion.Name,
            Description = promotion.Description,
            PromotionType = promotion.PromotionType.ToString(),
            DiscountType = promotion.DiscountType.ToString(),
            DiscountValue = promotion.DiscountValue,
            MinOrderAmount = promotion.MinOrderAmount,
            StartsAt = promotion.StartsAt,
            EndsAt = promotion.EndsAt,
            IsActive = promotion.IsActive,
            DishIds = promotion.PromotionDishes.Select(pd => pd.DishId).ToList(),
            CreatedAt = promotion.CreatedAt
        };
    }
}
