namespace DeliverTableSharedLibrary.Enums;

public enum TransactionType
{
    Credit,
    Withdrawal,
    DisputeReversal = 100,
    DisputeRestored = 101,
    Refund
}
