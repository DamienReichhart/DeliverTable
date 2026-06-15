using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableTests.Infrastructure.Unit.Payments;

[TestFixture]
public class PaymentLifecycleServiceTests
{
    private IOrderRepository _orderRepository = null!;
    private IPaymentRepository _paymentRepository = null!;
    private ILoyaltyRepository _loyaltyRepository = null!;
    private IDiscountCodeRepository _discountRepository = null!;
    private IStripeGateway _stripe = null!;
    private PaymentLifecycleService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _orderRepository = Substitute.For<IOrderRepository>();
        _paymentRepository = Substitute.For<IPaymentRepository>();
        _loyaltyRepository = Substitute.For<ILoyaltyRepository>();
        _discountRepository = Substitute.For<IDiscountCodeRepository>();
        _stripe = Substitute.For<IStripeGateway>();
        _sut = new PaymentLifecycleService(
            _orderRepository, _paymentRepository, _loyaltyRepository, _discountRepository, _stripe);
    }

    [Test]
    public async Task CancelAbandonedOrderAsync_TransitionsOrderAndCancelsIntent()
    {
        Order order = new Order { Id = 42, Status = OrderStatus.AwaitingPayment };
        Payment payment = new Payment { Id = 1, OrderId = 42, StripePaymentIntentId = "pi_123" };
        _orderRepository.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepository.GetByOrderIdAsync(42, Arg.Any<CancellationToken>()).Returns(payment);
        _stripe.CancelPaymentIntentAsync("pi_123", Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(new StripeCancelResult("pi_123", "canceled"));

        bool changed = await _sut.CancelAbandonedOrderAsync(42, CancellationToken.None);

        Assert.That(changed, Is.True);
        Assert.That(order.Status, Is.EqualTo(OrderStatus.Cancelled));
        await _stripe.Received(1).CancelPaymentIntentAsync("pi_123", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _loyaltyRepository.Received(1).MarkPendingRedemptionsReversedForOrderAsync(42, Arg.Any<CancellationToken>());
        await _discountRepository.Received(1).MarkPendingRedemptionsReversedForOrderAsync(42, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelAbandonedOrderAsync_OrderNotInAwaitingPayment_ReturnsFalse()
    {
        Order order = new Order { Id = 42, Status = OrderStatus.Pending };
        _orderRepository.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(order);

        bool changed = await _sut.CancelAbandonedOrderAsync(42, CancellationToken.None);

        Assert.That(changed, Is.False);
        await _stripe.DidNotReceive().CancelPaymentIntentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AutoRefuseOrderAsync_TransitionsPendingToRefusedAndCancelsAuth()
    {
        Order order = new Order { Id = 7, Status = OrderStatus.Pending };
        Payment payment = new Payment { Id = 1, OrderId = 7, StripePaymentIntentId = "pi_77" };
        _orderRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepository.GetByOrderIdAsync(7, Arg.Any<CancellationToken>()).Returns(payment);
        _stripe.CancelPaymentIntentAsync("pi_77", Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(new StripeCancelResult("pi_77", "canceled"));

        bool changed = await _sut.AutoRefuseOrderAsync(7, CancellationToken.None);

        Assert.That(changed, Is.True);
        Assert.That(order.Status, Is.EqualTo(OrderStatus.Refused));
        Assert.That(order.PaymentStatus, Is.EqualTo(PaymentStatus.Failed));
    }
}
