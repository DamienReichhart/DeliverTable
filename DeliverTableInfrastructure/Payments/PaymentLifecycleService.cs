using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Payments;

public sealed class PaymentLifecycleService(
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    ILoyaltyRepository loyaltyRepository,
    IDiscountCodeRepository discountRepository,
    IStripeGateway stripe) : IPaymentLifecycleService
{
    public async Task<bool> CancelAbandonedOrderAsync(int orderId, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(orderId, ct);
        if (order is null || order.Status != OrderStatus.AwaitingPayment) return false;

        var payment = await paymentRepository.GetByOrderIdAsync(orderId, ct);
        if (payment is not null && !string.IsNullOrEmpty(payment.StripePaymentIntentId))
        {
            await stripe.CancelPaymentIntentAsync(
                payment.StripePaymentIntentId,
                idempotencyKey: $"order:{orderId}:cancel-abandoned",
                ct);
            payment.Status = PaymentGatewayStatus.Canceled;
            payment.CanceledAt = DateTime.UtcNow;
            await paymentRepository.UpdateAsync(payment, ct);
        }

        await loyaltyRepository.MarkPendingRedemptionsReversedForOrderAsync(orderId, ct);
        await discountRepository.MarkPendingRedemptionsReversedForOrderAsync(orderId, ct);

        order.Status = OrderStatus.Cancelled;
        order.PaymentStatus = PaymentStatus.Failed;
        order.UpdatedAt = DateTime.UtcNow;
        await orderRepository.UpdateAsync(order, ct);

        return true;
    }

    public async Task<bool> AutoRefuseOrderAsync(int orderId, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(orderId, ct);
        if (order is null || order.Status != OrderStatus.Pending) return false;

        var payment = await paymentRepository.GetByOrderIdAsync(orderId, ct);
        if (payment is not null && !string.IsNullOrEmpty(payment.StripePaymentIntentId))
        {
            await stripe.CancelPaymentIntentAsync(
                payment.StripePaymentIntentId,
                idempotencyKey: $"order:{orderId}:auto-refuse",
                ct);
            payment.Status = PaymentGatewayStatus.Canceled;
            payment.CanceledAt = DateTime.UtcNow;
            await paymentRepository.UpdateAsync(payment, ct);
        }

        order.Status = OrderStatus.Refused;
        order.PaymentStatus = PaymentStatus.Failed;
        order.UpdatedAt = DateTime.UtcNow;
        await orderRepository.UpdateAsync(order, ct);

        return true;
    }
}
