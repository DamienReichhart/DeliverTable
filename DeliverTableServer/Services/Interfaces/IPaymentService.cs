using DeliverTableServer.Common;

namespace DeliverTableServer.Services.Interfaces;

public interface IPaymentService
{
    Task<ServiceResult<CreateIntentResult>> CreateIntentAsync(int orderId, CancellationToken ct);
}

public sealed record CreateIntentResult(string ClientSecret, string PaymentIntentId, decimal Amount, string Currency);
