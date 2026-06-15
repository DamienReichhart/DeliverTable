using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Extensions;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Hubs;
using DeliverTableServer.Hubs.Interfaces;
using DeliverTableServer.Mappers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Dtos.Payment;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

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
    IInvoiceService invoiceService,
    IDisputeService disputeService,
    ICommissionStatementService commissionStatementService,
    IMessagePublisher publisher,
    IHubContext<OrderHub, IOrderHub> hubContext,
    DeliverTableContext dbContext,
    AppEnvironment env,
    ILogger<PaymentService> logger) : IPaymentService
{
    private readonly AppEnvironment _env = env;
    private readonly DeliverTableContext _dbContext = dbContext;
    private readonly ILogger<PaymentService> _logger = logger;

    public async Task<ServiceResult<CreateIntentResult>> CreateIntentAsync(int orderId, CancellationToken ct)
    {
        _logger.LogInformation("Creating PaymentIntent for order {OrderId}", orderId);

        Order? order = await orderRepository.GetByIdAsync(orderId, ct);
        if (order is null) return new ServiceError(ErrorMessages.OrderNotFound);
        if (order.Status != OrderStatus.AwaitingPayment)
            return new ServiceError(ErrorMessages.OrderPaymentAlreadyProcessed);

        User? user = await userRepository.GetByIdAsync(order.CustomerId, ct);
        if (user is null) return new ServiceError(ErrorMessages.PaymentIntentCreationFailed);

        string stripeCustomerId = user.StripeCustomerId ?? string.Empty;
        if (string.IsNullOrEmpty(stripeCustomerId))
        {
            StripeCustomerResult customerResult = await stripe.CreateCustomerAsync(
                email: user.Email ?? string.Empty,
                fullName: user.GetFullName(),
                metadata: new Dictionary<string, string> { ["userId"] = user.Id.ToString() },
                ct);
            stripeCustomerId = customerResult.CustomerId;
            user.StripeCustomerId = stripeCustomerId;
            await userRepository.UpdateAsync(user, ct);
        }

        long amountMinor = (long)Math.Round(order.TotalAmount * 100m, MidpointRounding.AwayFromZero);
        Dictionary<string, string> metadata = new Dictionary<string, string>
        {
            ["orderId"] = order.Id.ToString(),
            ["userId"] = user.Id.ToString(),
            ["restaurantId"] = order.RestaurantId.ToString(),
        };

        StripePaymentIntentResult intent = await stripe.CreatePaymentIntentAsync(
            amountInMinorUnits: amountMinor,
            currency: "eur",
            stripeCustomerId: stripeCustomerId,
            metadata: metadata,
            idempotencyKey: $"order:{order.Id}:create-intent",
            ct);

        _logger.LogInformation("PaymentIntent {PaymentIntentId} created for order {OrderId}, amount {Amount}",
            intent.PaymentIntentId, order.Id, order.TotalAmount);

        Payment payment = new Payment
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
        Payment? payment = await paymentRepository.GetByOrderIdAsync(orderId, ct);
        if (payment is null) return new ServiceError(ErrorMessages.PaymentNotFound);
        try
        {
            _logger.LogInformation("Capturing payment intent {PaymentIntentId} for order {OrderId}",
                payment.StripePaymentIntentId, orderId);

            StripeCaptureResult capture = await stripe.CapturePaymentIntentAsync(
                payment.StripePaymentIntentId,
                idempotencyKey: $"order:{orderId}:capture",
                ct);
            payment.CapturedAt = DateTime.UtcNow;
            payment.Status = capture.Status == "succeeded"
                ? PaymentGatewayStatus.Succeeded
                : payment.Status;
            await paymentRepository.UpdateAsync(payment, ct);

            // Eagerly mark order as Completed so late-cancellation logic in UpdateStatusAsync
            // doesn't miss the refund branch before the payment_intent.succeeded webhook arrives.
            Order? order = await orderRepository.GetByIdAsync(orderId, ct);
            if (order is not null && order.PaymentStatus == PaymentStatus.Authorized)
            {
                order.PaymentStatus = PaymentStatus.Completed;
                order.UpdatedAt = DateTime.UtcNow;
                await orderRepository.UpdateAsync(order, ct);
            }

            return ServiceResult.Success();
        }
        catch (Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe capture failed for order {OrderId}", orderId);
            return new ServiceError(ErrorMessages.PaymentCaptureFailed + " " + ex.Message);
        }
    }

    public async Task<ServiceResult> CancelAuthorizationAsync(int orderId, int customerId, CancellationToken ct)
    {
        Order? order = await orderRepository.GetByIdAsync(orderId, ct);
        if (order is null) return new ServiceError(ErrorMessages.PaymentNotFound);
        if (order.CustomerId != customerId) return ServiceError.Forbidden(ErrorMessages.OrderAccessDenied);
        return await CancelAuthorizationAsync(orderId, ct);
    }

    public async Task<ServiceResult> CancelAuthorizationAsync(int orderId, CancellationToken ct)
    {
        Payment? payment = await paymentRepository.GetByOrderIdAsync(orderId, ct);
        if (payment is null) return new ServiceError(ErrorMessages.PaymentNotFound);
        try
        {
            _logger.LogInformation("Canceling payment intent {PaymentIntentId} for order {OrderId}",
                payment.StripePaymentIntentId, orderId);

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
            _logger.LogError(ex, "Stripe cancel authorization failed for order {OrderId}", orderId);
            return new ServiceError(ErrorMessages.PaymentCancelFailed + " " + ex.Message);
        }
    }

    public async Task<ServiceResult<RefundDto>> RefundAsync(int orderId, decimal amount, string reason, int? adminUserId, CancellationToken ct)
    {
        if (amount <= 0m) return new ServiceError(ErrorMessages.PaymentRefundFailed);

        Payment? payment = await paymentRepository.GetByOrderIdAsync(orderId, ct);
        if (payment is null) return new ServiceError(ErrorMessages.PaymentNotFound);

        if (await disputeService.HasOpenDisputeForOrderAsync(orderId, ct))
            return new ServiceError(ErrorMessages.RefundBlockedByOpenDispute);

        decimal alreadyRefunded = await paymentRepository.GetTotalRefundedAsync(payment.Id, ct);
        decimal remaining = payment.Amount - alreadyRefunded;
        if (remaining <= 0m) return new ServiceError(ErrorMessages.PaymentAlreadyRefunded);
        if (amount > remaining) return new ServiceError(ErrorMessages.PaymentRefundExceedsAmount);

        long amountMinor = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        string idempotencyKey = $"order:{orderId}:refund:{DateTime.UtcNow.Ticks}";

        _logger.LogInformation("Issuing refund of {Amount} for order {OrderId} via intent {PaymentIntentId}",
            amount, orderId, payment.StripePaymentIntentId);

        StripeRefundResult stripeRefund;
        try
        {
            stripeRefund = await stripe.CreateRefundAsync(
                payment.StripePaymentIntentId, amountMinor, idempotencyKey, ct);
        }
        catch (Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe refund failed for order {OrderId}", orderId);
            return new ServiceError(ErrorMessages.PaymentRefundFailed + " " + ex.Message);
        }

        Refund refund = new Refund
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

        Order? order = await orderRepository.GetByIdAsync(orderId, ct);
        if (order is not null)
        {
            decimal totalAfter = alreadyRefunded + amount;
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
        _logger.LogInformation("Dispatching Stripe event {EventId} of type {EventType}", evt.Id, evt.Type);

        using IDbContextTransaction tx = await _dbContext.Database.BeginTransactionAsync(ct);

        bool registered = await paymentRepository.TryRegisterProcessedEventAsync(evt.Id, evt.Type, ct);
        if (!registered)
        {
            await tx.CommitAsync(ct);
            return ServiceResult.Success();
        }

        // Collect deferred publish actions: they must run AFTER the transaction commits so that
        // RabbitMQ messages are never enqueued for DB writes that were ultimately rolled back.
        List<Func<Task>> deferredPublishes = new List<Func<Task>>();

        switch (evt.Type)
        {
            case "payment_intent.amount_capturable_updated":
                await HandleAuthorizationCompletedAsync((Stripe.PaymentIntent)evt.Data.Object, deferredPublishes, ct);
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
                await HandleChargeRefundedAsync((Stripe.Charge)evt.Data.Object, deferredPublishes, ct);
                break;
            case "charge.dispute.created":
                await disputeService.HandleCreatedAsync(
                    (Stripe.Dispute)evt.Data.Object, deferredPublishes, ct);
                break;
            case "charge.dispute.updated":
                await disputeService.HandleUpdatedAsync((Stripe.Dispute)evt.Data.Object, ct);
                break;
            case "charge.dispute.closed":
                await disputeService.HandleClosedAsync(
                    (Stripe.Dispute)evt.Data.Object, deferredPublishes, ct);
                break;
            case "charge.dispute.funds_withdrawn":
            case "charge.dispute.funds_reinstated":
                _logger.LogInformation(
                    "Dispute funds event {Type} acknowledged (state derived from created/closed)", evt.Type);
                break;
            case "charge.dispute.warning_needs_response":
            case "charge.dispute.warning_under_review":
            case "charge.dispute.warning_closed":
                _logger.LogInformation(
                    "Dispute warning event {Type} acknowledged (out of scope)", evt.Type);
                break;
            default:
                _logger.LogInformation("Stripe event {EventId} of type {EventType} not handled", evt.Id, evt.Type);
                break;
        }

        await tx.CommitAsync(ct);

        // Execute email publishes after the transaction has committed.
        foreach (Func<Task> publish in deferredPublishes)
            await publish();

        return ServiceResult.Success();
    }

    private async Task HandleAuthorizationCompletedAsync(
        Stripe.PaymentIntent pi, List<Func<Task>> deferredPublishes, CancellationToken ct)
    {
        Payment? payment = await paymentRepository.GetByStripePaymentIntentIdAsync(pi.Id, ct);
        if (payment is null) return;
        payment.AuthorizedAt = DateTime.UtcNow;
        await paymentRepository.UpdateAsync(payment, ct);

        Order? order = await orderRepository.GetByIdAsync(payment.OrderId, ct);
        if (order is null || order.Status != OrderStatus.AwaitingPayment) return;

        await loyaltyRepository.MarkPendingRedemptionsCommittedForOrderAsync(order.Id, ct);
        await discountRepository.MarkPendingRedemptionsCommittedForOrderAsync(order.Id, ct);
        await discountRepository.IncrementRedemptionCountersForCommittedAsync(order.Id, ct);

        order.Status = OrderStatus.Pending;
        order.PaymentStatus = PaymentStatus.Authorized;
        order.UpdatedAt = DateTime.UtcNow;
        await orderRepository.UpdateAsync(order, ct);

        List<Cart> carts = await cartRepository.GetByCustomerAsync(order.CustomerId, ct);
        foreach (Cart cart in carts)
            await cartRepository.DeleteAsync(cart.Id, ct);

        // Load email data while still inside the transaction, then enqueue after commit.
        Order? fullOrder = await orderRepository.GetByIdWithFullDetailsAsync(order.Id, ct);
        if (fullOrder?.Customer is not null && !string.IsNullOrWhiteSpace(fullOrder.Customer.Email))
        {
            string customerName = fullOrder.Customer.GetFullName();
            string email = fullOrder.Customer.Email;
            Order orderSnapshot = fullOrder;
            deferredPublishes.Add(() => emailJobService.QueueOrderConfirmationAsync(orderSnapshot, email, customerName));
        }

        if (fullOrder?.Restaurant is not null)
        {
            User? restaurantOwner = await userRepository.GetByIdAsync(fullOrder.Restaurant.OwnerId, ct);
            if (restaurantOwner is not null && !string.IsNullOrWhiteSpace(restaurantOwner.Email))
            {
                string ownerEmail = restaurantOwner.Email;
                string restaurantName = fullOrder.Restaurant.Name;
                Order orderSnapshot = fullOrder;
                deferredPublishes.Add(() => emailJobService.QueueNewOrderForRestaurantAsync(orderSnapshot, ownerEmail, restaurantName));
            }
        }

        ServiceResult<List<InvoiceJobMessage>> invoicesResult = await invoiceService.CreatePendingInvoicesForCapturedOrderAsync(order.Id, ct);
        if (invoicesResult is { IsSuccess: true, Value: not null })
        {
            foreach (InvoiceJobMessage msg in invoicesResult.Value)
            {
                InvoiceJobMessage captured = msg;
                deferredPublishes.Add(() => publisher.PublishAsync(MessagingExchanges.Invoice, captured, ct));
            }
        }

        if (fullOrder is not null)
        {
            OrderDto orderDto = fullOrder.ToDto();
            deferredPublishes.Add(() => hubContext.Clients.Group($"restaurant_{orderDto.RestaurantId}").NewOrder(orderDto));
        }
    }

    private async Task HandleCaptureCompletedAsync(Stripe.PaymentIntent pi, CancellationToken ct)
    {
        Payment? payment = await paymentRepository.GetByStripePaymentIntentIdAsync(pi.Id, ct);
        if (payment is null) return;
        payment.CapturedAt ??= DateTime.UtcNow;
        payment.Status = PaymentGatewayStatus.Succeeded;
        await paymentRepository.UpdateAsync(payment, ct);

        Order? order = await orderRepository.GetByIdAsync(payment.OrderId, ct);
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
        Payment? payment = await paymentRepository.GetByStripePaymentIntentIdAsync(pi.Id, ct);
        if (payment is null) return;
        payment.Status = PaymentGatewayStatus.Canceled;
        payment.CanceledAt = DateTime.UtcNow;
        await paymentRepository.UpdateAsync(payment, ct);

        Order? order = await orderRepository.GetByIdAsync(payment.OrderId, ct);
        if (order is null || order.Status != OrderStatus.AwaitingPayment) return;
        await loyaltyRepository.MarkPendingRedemptionsReversedForOrderAsync(order.Id, ct);
        await discountRepository.MarkPendingRedemptionsReversedForOrderAsync(order.Id, ct);
        order.Status = OrderStatus.Cancelled;
        order.PaymentStatus = failed ? PaymentStatus.Failed : PaymentStatus.Pending;
        order.UpdatedAt = DateTime.UtcNow;
        await orderRepository.UpdateAsync(order, ct);
    }

    private async Task HandleChargeRefundedAsync(Stripe.Charge charge, List<Func<Task>> deferredPublishes, CancellationToken ct)
    {
        Payment? payment = await paymentRepository.GetByStripePaymentIntentIdAsync(charge.PaymentIntentId, ct);
        if (payment is null) return;
        if (charge.Refunds?.Data is null) return;

        List<int> newRefundIds = new List<int>();

        foreach (Stripe.Refund? r in charge.Refunds.Data)
        {
            Refund? existing = await paymentRepository.GetRefundByStripeIdAsync(r.Id, ct);
            if (existing is not null) continue;
            Refund refund = new Refund
            {
                PaymentId = payment.Id,
                StripeRefundId = r.Id,
                Amount = (decimal)r.Amount / 100m,
                Currency = r.Currency.ToUpperInvariant(),
                Reason = r.Reason ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
            };
            await paymentRepository.AddRefundAsync(refund, ct);
            newRefundIds.Add(refund.Id);
        }

        Order? order = await orderRepository.GetByIdAsync(payment.OrderId, ct);
        if (order is null) return;
        decimal totalRefunded = await paymentRepository.GetTotalRefundedAsync(payment.Id, ct);
        order.PaymentStatus = totalRefunded >= payment.Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
        order.UpdatedAt = DateTime.UtcNow;
        await orderRepository.UpdateAsync(order, ct);

        foreach (int newRefundId in newRefundIds)
        {
            ServiceResult<List<InvoiceJobMessage>> cnResult = await invoiceService.CreateCreditNotesForRefundAsync(newRefundId, ct);
            if (cnResult is { IsSuccess: true, Value: not null })
            {
                foreach (InvoiceJobMessage msg in cnResult.Value)
                {
                    InvoiceJobMessage captured = msg;
                    deferredPublishes.Add(() => publisher.PublishAsync(MessagingExchanges.Invoice, captured, ct));
                }
            }

            Refund? newRefund = await paymentRepository.GetRefundByIdAsync(newRefundId, ct);
            if (newRefund is not null)
            {
                await commissionStatementService.HandleRefundForPriorPeriodAsync(
                    order.Id, newRefund.Id, newRefund.StripeRefundId, newRefund.Amount, ct);
            }
        }
    }
}
