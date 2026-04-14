using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Services.Interfaces;
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
}
