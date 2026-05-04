using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Helpers;

public static class DiscountValidationHelper
{
    public static ServiceError? ValidateDiscountType(
        string discountTypeValue, decimal discountValue, out DiscountType discountType)
    {
        discountType = default;

        if (!Enum.TryParse(discountTypeValue, ignoreCase: true, out discountType))
            return new ServiceError(ErrorMessages.InvalidFields);

        if (discountType == DiscountType.Percentage && discountValue > 100)
            return new ServiceError(ErrorMessages.PercentageDiscountTooHigh);

        return null;
    }

    public static ServiceError? ValidateDateRange(DateTime startsAt, DateTime endsAt) =>
        ValidateDateRange(startsAt, endsAt, ErrorMessages.InvalidPromotionDates);

    public static ServiceError? ValidateDateRange(DateTime startsAt, DateTime endsAt, string invalidDatesMessage)
    {
        if (endsAt <= startsAt)
            return new ServiceError(invalidDatesMessage);

        return null;
    }

    public static ServiceError? ValidatePercentageDiscount(DiscountType discountType, decimal discountValue)
    {
        if (discountType == DiscountType.Percentage && discountValue > 100)
            return new ServiceError(ErrorMessages.PercentageDiscountTooHigh);

        return null;
    }
}
