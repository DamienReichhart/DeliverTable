using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Mappers;

public static class AdminDiscountCodeMapper
{
    public static AdminDiscountCodeResponse ToAdminDto(this DiscountCode code)
    {
        return new AdminDiscountCodeResponse
        {
            Id = code.Id,
            Code = code.Code,
            Description = code.Description,
            DiscountType = code.DiscountType,
            DiscountValue = code.DiscountValue,
            MinOrderAmount = code.MinOrderAmount,
            ValidFrom = code.ValidFrom,
            ValidUntil = code.ValidUntil,
            MaxRedemptions = code.MaxRedemptions,
            PerUserLimit = code.PerUserLimit,
            CurrentRedemptions = code.CurrentRedemptions,
            IsActive = code.IsActive,
            RestaurantId = code.RestaurantId,
            RestaurantName = code.Restaurant is not null
                ? code.Restaurant.Name
                : "",
            RedemptionCount = code.Redemptions?.Count ?? 0,
            CreatedAt = code.CreatedAt,
            UpdatedAt = code.UpdatedAt
        };
    }

    public static AdminRedemptionResponse ToAdminDto(this DiscountCodeRedemption redemption)
    {
        return new AdminRedemptionResponse
        {
            Id = redemption.Id,
            CustomerName = redemption.Customer is not null
                ? $"{redemption.Customer.FirstName} {redemption.Customer.LastName}"
                : "",
            OrderId = redemption.OrderId,
            CreatedAt = redemption.CreatedAt
        };
    }
}
