using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Configuration;
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Factories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class PaymentServiceTests
{
    private IStripeGateway _stripe = null!;
    private IPaymentRepository _paymentRepo = null!;
    private IOrderRepository _orderRepo = null!;
    private IUserRepository _userRepo = null!;
    private ILoyaltyRepository _loyaltyRepo = null!;
    private IDiscountCodeRepository _discountRepo = null!;
    private ICartRepository _cartRepo = null!;
    private IEmailJobService _emailJobService = null!;
    private AppEnvironment _env = null!;
    private PaymentService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _stripe = Substitute.For<IStripeGateway>();
        _paymentRepo = Substitute.For<IPaymentRepository>();
        _orderRepo = Substitute.For<IOrderRepository>();
        _userRepo = Substitute.For<IUserRepository>();
        _loyaltyRepo = Substitute.For<ILoyaltyRepository>();
        _discountRepo = Substitute.For<IDiscountCodeRepository>();
        _cartRepo = Substitute.For<ICartRepository>();
        _emailJobService = Substitute.For<IEmailJobService>();
        _env = TestEnvironmentFactory.Create();
        _sut = new PaymentService(
            _stripe, _paymentRepo, _orderRepo, _userRepo,
            _loyaltyRepo, _discountRepo, _cartRepo, _emailJobService, _env);
    }

    [Test]
    public async Task CreateIntentAsync_NewStripeCustomer_PersistsCustomerIdAndCreatesIntent()
    {
        var user = new User { Id = 1, Email = "a@b.fr", FirstName = "A", LastName = "B" };
        var order = new Order
        {
            Id = 10,
            CustomerId = 1,
            RestaurantId = 5,
            TotalAmount = 12.50m,
            Status = OrderStatus.AwaitingPayment,
            Customer = user
        };
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        _stripe.CreateCustomerAsync("a@b.fr", "A B", Arg.Any<IDictionary<string, string>?>(), Arg.Any<CancellationToken>())
               .Returns(new StripeCustomerResult("cus_abc"));
        _stripe.CreatePaymentIntentAsync(
                 1250, "eur", "cus_abc",
                 Arg.Any<IDictionary<string, string>>(),
                 "order:10:create-intent",
                 Arg.Any<CancellationToken>())
               .Returns(new StripePaymentIntentResult("pi_1", "pi_1_secret_abc", "requires_payment_method"));

        var result = await _sut.CreateIntentAsync(10, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.ClientSecret, Is.EqualTo("pi_1_secret_abc"));
        Assert.That(user.StripeCustomerId, Is.EqualTo("cus_abc"));
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _paymentRepo.Received(1).CreateAsync(
            Arg.Is<Payment>(p => p.OrderId == 10 && p.StripePaymentIntentId == "pi_1" && p.Amount == 12.50m),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateIntentAsync_OrderNotInAwaitingPayment_ReturnsError()
    {
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(new Order { Id = 10, Status = OrderStatus.Confirmed });

        var result = await _sut.CreateIntentAsync(10, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public async Task CaptureAsync_HappyPath_CapturesIntentAndUpdatesPayment()
    {
        var payment = new Payment
        {
            Id = 1,
            OrderId = 10,
            StripePaymentIntentId = "pi_cap",
            Status = PaymentGatewayStatus.RequiresConfirmation,
            Amount = 15m,
        };
        _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
        _stripe.CapturePaymentIntentAsync("pi_cap", "order:10:capture", Arg.Any<CancellationToken>())
               .Returns(new StripeCaptureResult("pi_cap", "succeeded"));

        var result = await _sut.CaptureAsync(10, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(payment.CapturedAt, Is.Not.Null);
        await _paymentRepo.Received(1).UpdateAsync(payment, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CaptureAsync_StripeFails_ReturnsErrorAndDoesNotUpdate()
    {
        var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_fail" };
        _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
        _stripe.CapturePaymentIntentAsync("pi_fail", Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Throws(new Stripe.StripeException("boom"));

        var result = await _sut.CaptureAsync(10, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        await _paymentRepo.DidNotReceive().UpdateAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelAuthorizationAsync_CancelsIntentAndUpdatesPayment()
    {
        var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_c" };
        _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
        _stripe.CancelPaymentIntentAsync("pi_c", "order:10:cancel-auth", Arg.Any<CancellationToken>())
               .Returns(new StripeCancelResult("pi_c", "canceled"));

        var result = await _sut.CancelAuthorizationAsync(10, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(payment.Status, Is.EqualTo(PaymentGatewayStatus.Canceled));
        await _paymentRepo.Received(1).UpdateAsync(payment, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RefundAsync_HappyPath_PersistsRefundAndUpdatesOrderStatus()
    {
        var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_r", Amount = 50m };
        var order = new Order { Id = 10, PaymentStatus = PaymentStatus.Completed };
        _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepo.GetTotalRefundedAsync(1, Arg.Any<CancellationToken>()).Returns(0m);
        _stripe.CreateRefundAsync("pi_r", 2500, Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(new StripeRefundResult("re_1", "pi_r", 25m, "eur", "succeeded"));
        _paymentRepo.AddRefundAsync(Arg.Any<Refund>(), Arg.Any<CancellationToken>())
                    .Returns(ci => ci.Arg<Refund>());

        var result = await _sut.RefundAsync(10, 25m, "customer request", adminUserId: 99, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Amount, Is.EqualTo(25m));
        Assert.That(order.PaymentStatus, Is.EqualTo(PaymentStatus.PartiallyRefunded));
    }

    [Test]
    public async Task RefundAsync_FullRefund_SetsStatusRefunded()
    {
        var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_r", Amount = 50m };
        var order = new Order { Id = 10, PaymentStatus = PaymentStatus.Completed };
        _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepo.GetTotalRefundedAsync(1, Arg.Any<CancellationToken>()).Returns(0m);
        _stripe.CreateRefundAsync("pi_r", 5000, Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(new StripeRefundResult("re_full", "pi_r", 50m, "eur", "succeeded"));
        _paymentRepo.AddRefundAsync(Arg.Any<Refund>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Refund>());

        await _sut.RefundAsync(10, 50m, "full", adminUserId: null, CancellationToken.None);

        Assert.That(order.PaymentStatus, Is.EqualTo(PaymentStatus.Refunded));
    }

    [Test]
    public async Task RefundAsync_AmountExceedsRemaining_ReturnsError()
    {
        var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_r", Amount = 50m };
        _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
        _paymentRepo.GetTotalRefundedAsync(1, Arg.Any<CancellationToken>()).Returns(45m);

        var result = await _sut.RefundAsync(10, 10m, "x", adminUserId: 99, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Does.Contain("dépasse"));
        await _stripe.DidNotReceive().CreateRefundAsync(
            Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleStripeEventAsync_DuplicateEvent_ReturnsSuccessWithoutWork()
    {
        var evt = new Stripe.Event { Id = "evt_dup", Type = "payment_intent.succeeded" };
        _paymentRepo.TryRegisterProcessedEventAsync("evt_dup", "payment_intent.succeeded", Arg.Any<CancellationToken>())
                    .Returns(false);

        var result = await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _paymentRepo.DidNotReceive().GetByStripePaymentIntentIdAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleStripeEventAsync_AmountCapturableUpdated_TransitionsOrderAndCommitsRedemptions()
    {
        var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_x", Status = PaymentGatewayStatus.RequiresConfirmation };
        var order = new Order { Id = 10, Status = OrderStatus.AwaitingPayment, PaymentStatus = PaymentStatus.Pending, CustomerId = 2 };
        var pi = new Stripe.PaymentIntent { Id = "pi_x" };
        var evt = new Stripe.Event { Id = "evt_1", Type = "payment_intent.amount_capturable_updated", Data = new Stripe.EventData { Object = pi } };
        _paymentRepo.TryRegisterProcessedEventAsync("evt_1", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _paymentRepo.GetByStripePaymentIntentIdAsync("pi_x", Arg.Any<CancellationToken>()).Returns(payment);
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _cartRepo.GetByCustomerAsync(2, Arg.Any<CancellationToken>()).Returns(new List<Cart>());

        var result = await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(order.Status, Is.EqualTo(OrderStatus.Pending));
        Assert.That(order.PaymentStatus, Is.EqualTo(PaymentStatus.Authorized));
        Assert.That(payment.AuthorizedAt, Is.Not.Null);
    }

    [Test]
    public async Task HandleStripeEventAsync_PaymentFailed_CancelsOrderAndReverses()
    {
        var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_f" };
        var order = new Order { Id = 10, Status = OrderStatus.AwaitingPayment };
        var pi = new Stripe.PaymentIntent { Id = "pi_f" };
        var evt = new Stripe.Event { Id = "evt_f", Type = "payment_intent.payment_failed", Data = new Stripe.EventData { Object = pi } };
        _paymentRepo.TryRegisterProcessedEventAsync("evt_f", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _paymentRepo.GetByStripePaymentIntentIdAsync("pi_f", Arg.Any<CancellationToken>()).Returns(payment);
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);

        await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

        Assert.That(order.Status, Is.EqualTo(OrderStatus.Cancelled));
        Assert.That(payment.Status, Is.EqualTo(PaymentGatewayStatus.Canceled));
    }

    [Test]
    public async Task HandleStripeEventAsync_ChargeRefunded_UpsertsRefundAndUpdatesStatus()
    {
        var payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_r", Amount = 100m };
        var order = new Order { Id = 10, PaymentStatus = PaymentStatus.Completed };
        var charge = new Stripe.Charge
        {
            Id = "ch_1",
            PaymentIntentId = "pi_r",
            Refunds = new Stripe.StripeList<Stripe.Refund>
            {
                Data = new List<Stripe.Refund>
                {
                    new() { Id = "re_1", Amount = 2500, Currency = "eur", Status = "succeeded" }
                }
            }
        };
        var evt = new Stripe.Event { Id = "evt_ref", Type = "charge.refunded", Data = new Stripe.EventData { Object = charge } };
        _paymentRepo.TryRegisterProcessedEventAsync("evt_ref", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _paymentRepo.GetByStripePaymentIntentIdAsync("pi_r", Arg.Any<CancellationToken>()).Returns(payment);
        _paymentRepo.GetRefundByStripeIdAsync("re_1", Arg.Any<CancellationToken>()).Returns((DeliverTableInfrastructure.Models.Refund?)null);
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepo.GetTotalRefundedAsync(1, Arg.Any<CancellationToken>()).Returns(25m);

        await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

        await _paymentRepo.Received(1).AddRefundAsync(
            Arg.Is<DeliverTableInfrastructure.Models.Refund>(r => r.StripeRefundId == "re_1" && r.Amount == 25m),
            Arg.Any<CancellationToken>());
        Assert.That(order.PaymentStatus, Is.EqualTo(PaymentStatus.PartiallyRefunded));
    }
}
