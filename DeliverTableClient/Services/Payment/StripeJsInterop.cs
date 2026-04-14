using Microsoft.JSInterop;

namespace DeliverTableClient.Services.Payment;

public class StripeJsInterop(IJSRuntime js) : IStripeJsInterop
{
    private IJSObjectReference? _module;

    public async Task InitializeAsync(string publishableKey)
    {
        _module ??= await js.InvokeAsync<IJSObjectReference>(
            "import", "./Pages/Checkout/Checkout/Checkout.razor.js");
        await _module.InvokeVoidAsync("initialize", publishableKey);
    }

    public async Task MountPaymentElementAsync(string clientSecret, string domElementId)
    {
        if (_module is null) throw new InvalidOperationException("InitializeAsync first");
        await _module.InvokeVoidAsync("mountPaymentElement", clientSecret, domElementId);
    }

    public async Task<StripeConfirmResult> ConfirmPaymentAsync(string returnUrl)
    {
        if (_module is null) throw new InvalidOperationException("InitializeAsync first");
        return await _module.InvokeAsync<StripeConfirmResult>("confirmPayment", returnUrl);
    }

    public async Task UnmountAsync()
    {
        if (_module is not null) await _module.InvokeVoidAsync("unmount");
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null) await _module.DisposeAsync();
    }
}
