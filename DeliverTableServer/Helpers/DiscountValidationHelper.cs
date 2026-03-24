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

    public static ServiceError? ValidateDateRange(DateTime startsAt, DateTime endsAt)
    {
        if (endsAt <= startsAt)
            return new ServiceError(ErrorMessages.InvalidPromotionDates);

        return null;
    }
}
