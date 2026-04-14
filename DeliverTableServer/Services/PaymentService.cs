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
}
