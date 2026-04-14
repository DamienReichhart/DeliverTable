using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Payment;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Services;

public class PaymentService(
    IStripeGateway stripe,
    IPaymentRepository paymentRepository,
    IOrderRepository orderRepository,
    IUserRepository userRepository,
    ILoyaltyRepository loyaltyRepository,
    IDiscountCodeRepository discountRepository,
    ICartRepository cartRepository,
    IEmailJobService emailJobService,
    AppEnvironment env) : IPaymentService
{
    private readonly AppEnvironment _env = env;

    public async Task<ServiceResult<CreateIntentResult>> CreateIntentAsync(int orderId, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(orderId, ct);
        if (order is null) return new ServiceError(ErrorMessages.OrderNotFound);
        if (order.Status != OrderStatus.AwaitingPayment)
            return new ServiceError(ErrorMessages.OrderPaymentAlreadyProcessed);

        var user = await userRepository.GetByIdAsync(order.CustomerId, ct);
        if (user is null) return new ServiceError(ErrorMessages.PaymentIntentCreationFailed);

        string stripeCustomerId = user.StripeCustomerId ?? string.Empty;
        if (string.IsNullOrEmpty(stripeCustomerId))
        {
            var customerResult = await stripe.CreateCustomerAsync(
                email: user.Email ?? string.Empty,
                fullName: $"{user.FirstName} {user.LastName}".Trim(),
                metadata: new Dictionary<string, string> { ["userId"] = user.Id.ToString() },
                ct);
            stripeCustomerId = customerResult.CustomerId;
            user.StripeCustomerId = stripeCustomerId;
            await userRepository.UpdateAsync(user, ct);
        }

        long amountMinor = (long)Math.Round(order.TotalAmount * 100m, MidpointRounding.AwayFromZero);
        var metadata = new Dictionary<string, string>
        {
            ["orderId"] = order.Id.ToString(),
            ["userId"] = user.Id.ToString(),
            ["restaurantId"] = order.RestaurantId.ToString(),
        };

        var intent = await stripe.CreatePaymentIntentAsync(
            amountInMinorUnits: amountMinor,
            currency: "eur",
            stripeCustomerId: stripeCustomerId,
            metadata: metadata,
            idempotencyKey: $"order:{order.Id}:create-intent",
            ct);

        var payment = new Payment
        {
            OrderId = order.Id,
            Provider = "Stripe",
            StripePaymentIntentId = intent.PaymentIntentId,
            StripeChargeId = string.Empty,
            Amount = order.TotalAmount,
            Currency = "EUR",
            Status = PaymentGatewayStatus.RequiresPaymentMethod,
        };
        await paymentRepository.CreateAsync(payment, ct);

        return ServiceResult<CreateIntentResult>.Success(
            new CreateIntentResult(intent.ClientSecret, intent.PaymentIntentId, order.TotalAmount, "EUR"));
    }

    public async Task<ServiceResult> CaptureAsync(int orderId, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByOrderIdAsync(orderId, ct);
        if (payment is null) return new ServiceError(ErrorMessages.PaymentNotFound);
        try
        {
            var capture = await stripe.CapturePaymentIntentAsync(
                payment.StripePaymentIntentId,
                idempotencyKey: $"order:{orderId}:capture",
                ct);
            payment.CapturedAt = DateTime.UtcNow;
            payment.Status = capture.Status == "succeeded"
                ? PaymentGatewayStatus.Succeeded
                : payment.Status;
            await paymentRepository.UpdateAsync(payment, ct);
            return ServiceResult.Success();
        }
        catch (Stripe.StripeException ex)
        {
            return new ServiceError(ErrorMessages.PaymentCaptureFailed + " " + ex.Message);
        }
    }

    public async Task<ServiceResult> CancelAuthorizationAsync(int orderId, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByOrderIdAsync(orderId, ct);
        if (payment is null) return new ServiceError(ErrorMessages.PaymentNotFound);
        try
        {
            await stripe.CancelPaymentIntentAsync(
                payment.StripePaymentIntentId,
                idempotencyKey: $"order:{orderId}:cancel-auth",
                ct);
            payment.Status = PaymentGatewayStatus.Canceled;
            payment.CanceledAt = DateTime.UtcNow;
            await paymentRepository.UpdateAsync(payment, ct);
            return ServiceResult.Success();
        }
        catch (Stripe.StripeException ex)
        {
            return new ServiceError(ErrorMessages.PaymentCancelFailed + " " + ex.Message);
        }
    }

    public async Task<ServiceResult<RefundDto>> RefundAsync(int orderId, decimal amount, string reason, int? adminUserId, CancellationToken ct)
    {
        if (amount <= 0m) return new ServiceError(ErrorMessages.PaymentRefundFailed);

        var payment = await paymentRepository.GetByOrderIdAsync(orderId, ct);
        if (payment is null) return new ServiceError(ErrorMessages.PaymentNotFound);

        var alreadyRefunded = await paymentRepository.GetTotalRefundedAsync(payment.Id, ct);
        var remaining = payment.Amount - alreadyRefunded;
        if (remaining <= 0m) return new ServiceError(ErrorMessages.PaymentAlreadyRefunded);
        if (amount > remaining) return new ServiceError(ErrorMessages.PaymentRefundExceedsAmount);

        long amountMinor = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        var idempotencyKey = $"order:{orderId}:refund:{DateTime.UtcNow.Ticks}";

        StripeRefundResult stripeRefund;
        try
        {
            stripeRefund = await stripe.CreateRefundAsync(
                payment.StripePaymentIntentId, amountMinor, idempotencyKey, ct);
        }
        catch (Stripe.StripeException ex)
        {
            return new ServiceError(ErrorMessages.PaymentRefundFailed + " " + ex.Message);
        }

        var refund = new Refund
        {
            PaymentId = payment.Id,
            StripeRefundId = stripeRefund.RefundId,
            Amount = amount,
            Currency = "EUR",
            Reason = reason,
            CreatedByUserId = adminUserId,
            CreatedAt = DateTime.UtcNow,
        };
        await paymentRepository.AddRefundAsync(refund, ct);

        var order = await orderRepository.GetByIdAsync(orderId, ct);
        if (order is not null)
        {
            var totalAfter = alreadyRefunded + amount;
            order.PaymentStatus = totalAfter >= payment.Amount
                ? PaymentStatus.Refunded
                : PaymentStatus.PartiallyRefunded;
            order.UpdatedAt = DateTime.UtcNow;
            await orderRepository.UpdateAsync(order, ct);
        }

        return ServiceResult<RefundDto>.Success(
            new RefundDto(refund.Id, refund.Amount, refund.Currency, refund.Reason, refund.CreatedAt));
    }

    public async Task<ServiceResult> HandleStripeEventAsync(Stripe.Event evt, CancellationToken ct)
    {
        var registered = await paymentRepository.TryRegisterProcessedEventAsync(evt.Id, evt.Type, ct);
        if (!registered) return ServiceResult.Success();

        switch (evt.Type)
        {
            case "payment_intent.amount_capturable_updated":
                await HandleAuthorizationCompletedAsync((Stripe.PaymentIntent)evt.Data.Object, ct);
                break;
            case "payment_intent.succeeded":
                await HandleCaptureCompletedAsync((Stripe.PaymentIntent)evt.Data.Object, ct);
                break;
            case "payment_intent.payment_failed":
            case "payment_intent.canceled":
                await HandlePaymentAbortedAsync(
                    (Stripe.PaymentIntent)evt.Data.Object,
                    failed: evt.Type == "payment_intent.payment_failed",
                    ct);
                break;
            case "charge.refunded":
                await HandleChargeRefundedAsync((Stripe.Charge)evt.Data.Object, ct);
                break;
            default:
                break;
        }
        return ServiceResult.Success();
    }

    private async Task HandleAuthorizationCompletedAsync(Stripe.PaymentIntent pi, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByStripePaymentIntentIdAsync(pi.Id, ct);
        if (payment is null) return;
        payment.AuthorizedAt = DateTime.UtcNow;
        await paymentRepository.UpdateAsync(payment, ct);

        var order = await orderRepository.GetByIdAsync(payment.OrderId, ct);
        if (order is null || order.Status != OrderStatus.AwaitingPayment) return;

        await loyaltyRepository.MarkPendingRedemptionsCommittedForOrderAsync(order.Id, ct);
        await discountRepository.MarkPendingRedemptionsCommittedForOrderAsync(order.Id, ct);

        order.Status = OrderStatus.Pending;
        order.PaymentStatus = PaymentStatus.Authorized;
        order.UpdatedAt = DateTime.UtcNow;
        await orderRepository.UpdateAsync(order, ct);

        var carts = await cartRepository.GetByCustomerAsync(order.CustomerId, ct);
        foreach (var cart in carts)
            await cartRepository.DeleteAsync(cart.Id, ct);

        var fullOrder = await orderRepository.GetByIdWithFullDetailsAsync(order.Id, ct);
        if (fullOrder?.Customer is not null && !string.IsNullOrWhiteSpace(fullOrder.Customer.Email))
        {
            var customerName = $"{fullOrder.Customer.FirstName} {fullOrder.Customer.LastName}".Trim();
            await emailJobService.QueueOrderConfirmationAsync(fullOrder, fullOrder.Customer.Email, customerName);
        }

        if (fullOrder?.Restaurant is not null)
        {
            var restaurantOwner = await userRepository.GetByIdAsync(fullOrder.Restaurant.OwnerId, ct);
            if (restaurantOwner is not null && !string.IsNullOrWhiteSpace(restaurantOwner.Email))
                await emailJobService.QueueNewOrderForRestaurantAsync(fullOrder, restaurantOwner.Email, fullOrder.Restaurant.Name);
        }
    }

    private async Task HandleCaptureCompletedAsync(Stripe.PaymentIntent pi, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByStripePaymentIntentIdAsync(pi.Id, ct);
        if (payment is null) return;
        payment.CapturedAt ??= DateTime.UtcNow;
        payment.Status = PaymentGatewayStatus.Succeeded;
        await paymentRepository.UpdateAsync(payment, ct);

        var order = await orderRepository.GetByIdAsync(payment.OrderId, ct);
        if (order is null) return;
        if (order.PaymentStatus != PaymentStatus.Refunded && order.PaymentStatus != PaymentStatus.PartiallyRefunded)
        {
            order.PaymentStatus = PaymentStatus.Completed;
            order.UpdatedAt = DateTime.UtcNow;
            await orderRepository.UpdateAsync(order, ct);
        }
    }

    private async Task HandlePaymentAbortedAsync(Stripe.PaymentIntent pi, bool failed, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByStripePaymentIntentIdAsync(pi.Id, ct);
        if (payment is null) return;
        payment.Status = PaymentGatewayStatus.Canceled;
        payment.CanceledAt = DateTime.UtcNow;
        await paymentRepository.UpdateAsync(payment, ct);

        var order = await orderRepository.GetByIdAsync(payment.OrderId, ct);
        if (order is null || order.Status != OrderStatus.AwaitingPayment) return;
        await loyaltyRepository.MarkPendingRedemptionsReversedForOrderAsync(order.Id, ct);
        await discountRepository.MarkPendingRedemptionsReversedForOrderAsync(order.Id, ct);
        order.Status = OrderStatus.Cancelled;
        order.PaymentStatus = failed ? PaymentStatus.Failed : PaymentStatus.Pending;
        order.UpdatedAt = DateTime.UtcNow;
        await orderRepository.UpdateAsync(order, ct);
    }

    private async Task HandleChargeRefundedAsync(Stripe.Charge charge, CancellationToken ct)
    {
        var payment = await paymentRepository.GetByStripePaymentIntentIdAsync(charge.PaymentIntentId, ct);
        if (payment is null) return;
        if (charge.Refunds?.Data is null) return;

        foreach (var r in charge.Refunds.Data)
        {
            var existing = await paymentRepository.GetRefundByStripeIdAsync(r.Id, ct);
            if (existing is not null) continue;
            var refund = new Refund
            {
                PaymentId = payment.Id,
                StripeRefundId = r.Id,
                Amount = (decimal)r.Amount / 100m,
                Currency = r.Currency.ToUpperInvariant(),
                Reason = r.Reason ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
            };
            await paymentRepository.AddRefundAsync(refund, ct);
        }

        var order = await orderRepository.GetByIdAsync(payment.OrderId, ct);
        if (order is null) return;
        var totalRefunded = await paymentRepository.GetTotalRefundedAsync(payment.Id, ct);
        order.PaymentStatus = totalRefunded >= payment.Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
        order.UpdatedAt = DateTime.UtcNow;
        await orderRepository.UpdateAsync(order, ct);
    }
}
