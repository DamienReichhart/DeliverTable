namespace DeliverTableSharedLibrary.Enums;

public enum PaymentGatewayStatus
{
    RequiresPaymentMethod,
    RequiresConfirmation,
    Succeeded,
    Canceled,
    Refunded
}
