namespace DeliverTableInfrastructure.Payments;

public interface IPaymentLifecycleService
{
    /// <summary>
    /// Cancels the Stripe PaymentIntent for an order stuck in AwaitingPayment,
    /// reverses loyalty/discount redemptions, and transitions the order to Cancelled.
    /// Returns true if any state changed; false if the order was not in AwaitingPayment.
    /// </summary>
    Task<bool> CancelAbandonedOrderAsync(int orderId, CancellationToken ct);

    /// <summary>
    /// Auto-refuses an order that has been Pending longer than the restaurant response window.
    /// Cancels the Stripe authorization (releases the hold). Returns true on state change.
    /// </summary>
    Task<bool> AutoRefuseOrderAsync(int orderId, CancellationToken ct);
}
