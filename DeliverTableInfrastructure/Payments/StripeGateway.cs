using Stripe;

namespace DeliverTableInfrastructure.Payments;

public class StripeGateway : IStripeGateway
{
    public async Task<StripeCustomerResult> CreateCustomerAsync(
        string email, string fullName, IDictionary<string, string>? metadata, CancellationToken ct)
    {
        var service = new CustomerService();
        var options = new CustomerCreateOptions
        {
            Email = email,
            Name = fullName,
            Metadata = metadata != null ? new Dictionary<string, string>(metadata) : null,
        };
        var customer = await service.CreateAsync(options, cancellationToken: ct);
        return new StripeCustomerResult(customer.Id);
    }

    public async Task<StripePaymentIntentResult> CreatePaymentIntentAsync(
        long amountInMinorUnits,
        string currency,
        string stripeCustomerId,
        IDictionary<string, string> metadata,
        string idempotencyKey,
        CancellationToken ct)
    {
        var service = new PaymentIntentService();
        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInMinorUnits,
            Currency = currency,
            Customer = stripeCustomerId,
            CaptureMethod = "manual",
            SetupFutureUsage = "off_session",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
            Metadata = new Dictionary<string, string>(metadata),
        };
        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        var pi = await service.CreateAsync(options, requestOptions, ct);
        return new StripePaymentIntentResult(pi.Id, pi.ClientSecret, pi.Status);
    }

    public async Task<StripeCaptureResult> CapturePaymentIntentAsync(
        string paymentIntentId, string idempotencyKey, CancellationToken ct)
    {
        var service = new PaymentIntentService();
        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        var pi = await service.CaptureAsync(paymentIntentId, requestOptions: requestOptions, cancellationToken: ct);
        return new StripeCaptureResult(pi.Id, pi.Status);
    }

    public async Task<StripeCancelResult> CancelPaymentIntentAsync(
        string paymentIntentId, string idempotencyKey, CancellationToken ct)
    {
        var service = new PaymentIntentService();
        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        var pi = await service.CancelAsync(paymentIntentId, requestOptions: requestOptions, cancellationToken: ct);
        return new StripeCancelResult(pi.Id, pi.Status);
    }

    public async Task<StripeRefundResult> CreateRefundAsync(
        string paymentIntentId, long amountInMinorUnits, string idempotencyKey, CancellationToken ct)
    {
        var service = new RefundService();
        var options = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId,
            Amount = amountInMinorUnits,
        };
        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        var refund = await service.CreateAsync(options, requestOptions, ct);
        return new StripeRefundResult(
            refund.Id,
            refund.PaymentIntentId,
            (decimal)refund.Amount / 100m,
            refund.Currency,
            refund.Status);
    }

    public Event ConstructWebhookEvent(string payload, string signatureHeader, string webhookSecret)
    {
        return EventUtility.ConstructEvent(payload, signatureHeader, webhookSecret);
    }
}
