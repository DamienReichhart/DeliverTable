using System.Globalization;

namespace DeliverTableClient.Helpers;

public static class DisplayHelpers
{
    public static readonly CultureInfo FrCulture = CultureInfo.GetCultureInfo("fr-FR");

    public static string TranslateOrderType(string orderType) => orderType switch
    {
        "Delivery" => "Livraison",
        "DineIn" => "Sur place",
        _ => orderType
    };

    public static string TranslateStatus(string status) => status switch
    {
        "Pending" => "En attente",
        "Confirmed" => "Confirmée",
        "Refused" => "Refusée",
        "Preparing" => "En préparation",
        "Ready" => "Prête",
        "Delivering" => "En livraison",
        "Delivered" => "Livrée",
        "Cancelled" => "Annulée",
        _ => status
    };

    public static string TranslatePaymentStatus(string status) => status switch
    {
        "Pending" => "En attente",
        "Completed" => "Payé",
        "Failed" => "Échoué",
        "Refunded" => "Remboursé",
        _ => status
    };

    public static string TranslateDiscountSource(string source) => source switch
    {
        "Promotion" => "Promotion",
        "DiscountCode" => "Code promo",
        "LoyaltyPoints" => "Fidélité",
        _ => source
    };

    public static string FormatDiscount(string discountType, decimal value) => discountType switch
    {
        "Percentage" => $"{value}%",
        "FixedAmount" => $"{value:0.00} €",
        _ => $"{value}"
    };

    public static string GetInitials(string firstName, string lastName)
    {
        var first = string.IsNullOrEmpty(firstName) ? "" : firstName[..1].ToUpperInvariant();
        var last = string.IsNullOrEmpty(lastName) ? "" : lastName[..1].ToUpperInvariant();
        return $"{first}{last}";
    }
}
