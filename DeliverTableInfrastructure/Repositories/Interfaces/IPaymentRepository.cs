using DeliverTableInfrastructure.Models;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface IPaymentRepository
{
    Task<Payment> CreateAsync(Payment payment, CancellationToken ct = default);
    Task<Payment?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken ct = default);
    Task<Payment?> GetByStripePaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default);
    Task UpdateAsync(Payment payment, CancellationToken ct = default);

    Task<Refund> AddRefundAsync(Refund refund, CancellationToken ct = default);
    Task<Refund?> GetRefundByIdAsync(int id, CancellationToken ct = default);
    Task<Refund?> GetRefundByStripeIdAsync(string stripeRefundId, CancellationToken ct = default);
    Task<decimal> GetTotalRefundedAsync(int paymentId, CancellationToken ct = default);

    /// <summary>Returns true if the event was inserted (first time), false if already present (idempotent replay).</summary>
    Task<bool> TryRegisterProcessedEventAsync(string stripeEventId, string eventType, CancellationToken ct = default);
}
