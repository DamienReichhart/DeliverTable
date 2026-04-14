namespace DeliverTableInfrastructure.Payments;

public sealed record StripeCustomerResult(string CustomerId);

public sealed record StripePaymentIntentResult(
    string PaymentIntentId,
    string ClientSecret,
    string Status);

public sealed record StripeCaptureResult(string PaymentIntentId, string Status);

public sealed record StripeCancelResult(string PaymentIntentId, string Status);

public sealed record StripeRefundResult(
    string RefundId,
    string PaymentIntentId,
    decimal Amount,
    string Currency,
    string Status);
