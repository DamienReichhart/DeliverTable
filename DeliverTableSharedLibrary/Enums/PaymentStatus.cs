namespace DeliverTableSharedLibrary.Enums;

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded,
    Authorized = 100,
    PartiallyRefunded = 101,
}
