using System.Globalization;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableClient.Helpers;

public static class DisplayHelpers
{
    public static readonly CultureInfo FrCulture = CultureInfo.GetCultureInfo("fr-FR");

    public static string TranslateOrderType(string orderType) => orderType switch
    {
        nameof(OrderType.Delivery) => "Livraison",
        nameof(OrderType.DineIn) => "Sur place",
        _ => orderType
    };

    public static string TranslateStatus(string status) => status switch
    {
        nameof(OrderStatus.Pending) => "En attente",
        nameof(OrderStatus.Confirmed) => "Confirmée",
        nameof(OrderStatus.Refused) => "Refusée",
        nameof(OrderStatus.Preparing) => "En préparation",
        nameof(OrderStatus.Ready) => "Prête",
        nameof(OrderStatus.Delivering) => "En livraison",
        nameof(OrderStatus.Delivered) => "Livrée",
        nameof(OrderStatus.Cancelled) => "Annulée",
        _ => status
    };

    public static string OrderStatusBadgeVariant(string status) => status switch
    {
        nameof(OrderStatus.Pending) => "warning",
        nameof(OrderStatus.Confirmed) => "info",
        nameof(OrderStatus.Preparing) => "info",
        nameof(OrderStatus.Ready) => "success",
        nameof(OrderStatus.Delivering) => "info",
        nameof(OrderStatus.Delivered) => "success",
        nameof(OrderStatus.Cancelled) => "danger",
        nameof(OrderStatus.Refused) => "danger",
        _ => "info"
    };

    public static string TranslatePaymentStatus(string status) => status switch
    {
        nameof(PaymentStatus.Pending) => "En attente",
        nameof(PaymentStatus.Completed) => "Payé",
        nameof(PaymentStatus.Failed) => "Échoué",
        nameof(PaymentStatus.Refunded) => "Remboursé",
        _ => status
    };

    public static string PaymentStatusBadgeVariant(string status) => status switch
    {
        nameof(PaymentStatus.Pending) => "warning",
        nameof(PaymentStatus.Completed) => "success",
        nameof(PaymentStatus.Failed) => "danger",
        nameof(PaymentStatus.Refunded) => "info",
        _ => "info"
    };

    public static string TranslateDiscountSource(string source) => source switch
    {
        nameof(OrderDiscountSource.Promotion) => "Promotion",
        nameof(OrderDiscountSource.DiscountCode) => "Code promo",
        nameof(OrderDiscountSource.LoyaltyPoints) => "Fidélité",
        _ => source
    };

    public static string FormatDiscount(string discountType, decimal value) => discountType switch
    {
        nameof(DiscountType.Percentage) => $"{value}%",
        nameof(DiscountType.FixedAmount) => $"{value:0.00} €",
        _ => $"{value}"
    };

    public static string GetInitials(string firstName, string lastName)
    {
        var first = string.IsNullOrEmpty(firstName) ? "" : firstName[..1].ToUpperInvariant();
        var last = string.IsNullOrEmpty(lastName) ? "" : lastName[..1].ToUpperInvariant();
        return $"{first}{last}";
    }
}
