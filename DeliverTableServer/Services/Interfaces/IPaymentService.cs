using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Payment;

namespace DeliverTableServer.Services.Interfaces;

public interface IPaymentService
{
    Task<ServiceResult<CreateIntentResult>> CreateIntentAsync(int orderId, CancellationToken ct);
    Task<ServiceResult> CaptureAsync(int orderId, CancellationToken ct);
    Task<ServiceResult> CancelAuthorizationAsync(int orderId, CancellationToken ct);
    Task<ServiceResult<RefundDto>> RefundAsync(int orderId, decimal amount, string reason, int? adminUserId, CancellationToken ct);
    Task<ServiceResult> HandleStripeEventAsync(Stripe.Event evt, CancellationToken ct);
}

public sealed record CreateIntentResult(string ClientSecret, string PaymentIntentId, decimal Amount, string Currency);
