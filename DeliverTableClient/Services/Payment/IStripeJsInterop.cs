namespace DeliverTableClient.Services.Payment;

public interface IStripeJsInterop : IAsyncDisposable
{
    Task InitializeAsync(string publishableKey);
    Task MountPaymentElementAsync(string clientSecret, string domElementId);
    Task<StripeConfirmResult> ConfirmPaymentAsync(string returnUrl);
    Task UnmountAsync();
}

public record StripeConfirmResult(bool Succeeded, string? ErrorMessage, string? PaymentIntentId);
