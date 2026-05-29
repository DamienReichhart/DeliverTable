namespace DeliverTableSharedLibrary.Enums;

public enum EmailJobType
{
    OrderConfirmation,
    OrderStatusUpdate,
    OrderDelivered,
    OrderCancelled,
    OrderReady,
    NewOrderForRestaurant,
    PasswordReset,
    PasswordChanged,
    WelcomeEmail,
    InvoiceReadyCustomer,
    InvoiceReadyRestaurant,
    DisputeOpenedAdmin,
    DisputeOpenedRestaurant,
    DisputeWonAdmin,
    DisputeWonRestaurant,
    DisputeLostAdmin,
    DisputeLostRestaurant,
    CommissionStatementInvoice,
    CommissionStatementCreditNote,
}
