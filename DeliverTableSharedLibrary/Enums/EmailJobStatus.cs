namespace DeliverTableSharedLibrary.Enums;

public enum EmailJobStatus
{
    Pending,
    Processing,
    Sent,
    Failed,
    RetryScheduled,
    DeadLettered
}
