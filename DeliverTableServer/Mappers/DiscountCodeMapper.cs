using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.DiscountCode;

namespace DeliverTableServer.Mappers;

public static class DiscountCodeMapper
{
    public static DiscountCodeDto ToDto(this DiscountCode code)
    {
        return new DiscountCodeDto
        {
            Id = code.Id,
            RestaurantId = code.RestaurantId,
            Code = code.Code,
            Description = code.Description,
            DiscountType = code.DiscountType.ToString(),
            DiscountValue = code.DiscountValue,
            MinOrderAmount = code.MinOrderAmount,
            ValidFrom = code.ValidFrom,
            ValidUntil = code.ValidUntil,
            MaxRedemptions = code.MaxRedemptions,
            PerUserLimit = code.PerUserLimit,
            CurrentRedemptions = code.CurrentRedemptions,
            IsActive = code.IsActive,
            CreatedAt = code.CreatedAt
        };
    }
}
