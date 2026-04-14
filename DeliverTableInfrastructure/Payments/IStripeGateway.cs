using Stripe;

namespace DeliverTableInfrastructure.Payments;

public interface IStripeGateway
{
    Task<StripeCustomerResult> CreateCustomerAsync(
        string email,
        string fullName,
        IDictionary<string, string>? metadata,
        CancellationToken ct);

    Task<StripePaymentIntentResult> CreatePaymentIntentAsync(
        long amountInMinorUnits,
        string currency,
        string stripeCustomerId,
        IDictionary<string, string> metadata,
        string idempotencyKey,
        CancellationToken ct);

    Task<StripeCaptureResult> CapturePaymentIntentAsync(
        string paymentIntentId,
        string idempotencyKey,
        CancellationToken ct);

    Task<StripeCancelResult> CancelPaymentIntentAsync(
        string paymentIntentId,
        string idempotencyKey,
        CancellationToken ct);

    Task<StripeRefundResult> CreateRefundAsync(
        string paymentIntentId,
        long amountInMinorUnits,
        string idempotencyKey,
        CancellationToken ct);

    Event ConstructWebhookEvent(string payload, string signatureHeader, string webhookSecret);
}
