using DeliverTableSharedLibrary.Dtos.DiscountCode;
using DeliverTableSharedLibrary.Dtos.Loyalty;

namespace DeliverTableClient.State;

/// <summary>
///     Holds the customer selections made on the Cart page so the Checkout page can display
///     them read-only and submit them to the API without re-prompting the user.
/// </summary>
public class CheckoutState
{
    public int RestaurantId { get; set; }
    public string OrderType { get; set; } = "Delivery";
    public int GuestCount { get; set; } = 1;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public List<DiscountCodeDto> AppliedCodes { get; set; } = [];
    public int LoyaltyPointsToRedeem { get; set; }
    public LoyaltyAccountDto? LoyaltyAccount { get; set; }

    public bool IsReady => RestaurantId > 0;

    public void Clear()
    {
        RestaurantId = 0;
        OrderType = "Delivery";
        GuestCount = 1;
        DeliveryAddress = string.Empty;
        Notes = string.Empty;
        ScheduledAt = null;
        AppliedCodes = [];
        LoyaltyPointsToRedeem = 0;
        LoyaltyAccount = null;
    }
}
