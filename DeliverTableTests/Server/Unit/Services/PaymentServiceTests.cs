using DeliverTableInfrastructure.Data;
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
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Payment;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Factories;
using DeliverTableTests.Server.Fixtures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private IInvoiceService _invoiceService = null!;
    private IDisputeService _disputeService = null!;
    private ICommissionStatementService _commissionStatementService = null!;
    private IMessagePublisher _publisher = null!;
    private IHubContext<OrderHub, IOrderHub> _hubContext = null!;
    private AppEnvironment _env = null!;
    private TestDatabase _testDb = null!;
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
        _invoiceService = Substitute.For<IInvoiceService>();
        _disputeService = Substitute.For<IDisputeService>();
        _commissionStatementService = Substitute.For<ICommissionStatementService>();
        _publisher = Substitute.For<IMessagePublisher>();
        _hubContext = Substitute.For<IHubContext<OrderHub, IOrderHub>>();
        _env = TestEnvironmentFactory.Create();
        _testDb = new TestDatabase();
        _sut = new PaymentService(
            _stripe, _paymentRepo, _orderRepo, _userRepo,
            _loyaltyRepo, _discountRepo, _cartRepo, _emailJobService, _invoiceService, _disputeService,
            _commissionStatementService, _publisher,
            _hubContext, _testDb.Context, _env, NullLogger<PaymentService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _testDb.Dispose();
    }

    [Test]
    public async Task CreateIntentAsync_NewStripeCustomer_PersistsCustomerIdAndCreatesIntent()
    {
        User user = new User { Id = 1, Email = "a@b.fr", FirstName = "A", LastName = "B" };
        Order order = new Order
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

        ServiceResult<CreateIntentResult> result = await _sut.CreateIntentAsync(10, CancellationToken.None);

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

        ServiceResult<CreateIntentResult> result = await _sut.CreateIntentAsync(10, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public async Task CaptureAsync_HappyPath_CapturesIntentAndUpdatesPayment()
    {
        Payment payment = new Payment
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

        ServiceResult result = await _sut.CaptureAsync(10, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(payment.CapturedAt, Is.Not.Null);
        await _paymentRepo.Received(1).UpdateAsync(payment, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CaptureAsync_SetsOrderPaymentStatusToCompleted()
    {
        Payment payment = new Payment
        {
            Id = 1,
            OrderId = 10,
            StripePaymentIntentId = "pi_cap2",
            Status = PaymentGatewayStatus.RequiresConfirmation,
            Amount = 20m,
        };
        Order order = new Order { Id = 10, PaymentStatus = PaymentStatus.Authorized };
        _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepo.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);
        _stripe.CapturePaymentIntentAsync("pi_cap2", "order:10:capture", Arg.Any<CancellationToken>())
               .Returns(new StripeCaptureResult("pi_cap2", "succeeded"));

        ServiceResult result = await _sut.CaptureAsync(10, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(order.PaymentStatus, Is.EqualTo(PaymentStatus.Completed));
        await _orderRepo.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CaptureAsync_StripeFails_ReturnsErrorAndDoesNotUpdate()
    {
        Payment payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_fail" };
        _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
        _stripe.CapturePaymentIntentAsync("pi_fail", Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Throws(new Stripe.StripeException("boom"));

        ServiceResult result = await _sut.CaptureAsync(10, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        await _paymentRepo.DidNotReceive().UpdateAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelAuthorizationAsync_CancelsIntentAndUpdatesPayment()
    {
        Payment payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_c" };
        _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
        _stripe.CancelPaymentIntentAsync("pi_c", "order:10:cancel-auth", Arg.Any<CancellationToken>())
               .Returns(new StripeCancelResult("pi_c", "canceled"));

        ServiceResult result = await _sut.CancelAuthorizationAsync(10, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(payment.Status, Is.EqualTo(PaymentGatewayStatus.Canceled));
        await _paymentRepo.Received(1).UpdateAsync(payment, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelAuthorizationAsync_WithCustomerId_OrderOwnedByCustomer_Succeeds()
    {
        Order order = new Order { Id = 10, CustomerId = 5 };
        Payment payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_c" };
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
        _stripe.CancelPaymentIntentAsync("pi_c", "order:10:cancel-auth", Arg.Any<CancellationToken>())
               .Returns(new StripeCancelResult("pi_c", "canceled"));

        ServiceResult result = await _sut.CancelAuthorizationAsync(10, 5, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(payment.Status, Is.EqualTo(PaymentGatewayStatus.Canceled));
    }

    [Test]
    public async Task CancelAuthorizationAsync_OrderNotOwnedByCustomer_ReturnsError()
    {
        Order order = new Order { Id = 10, CustomerId = 99 };
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);

        ServiceResult result = await _sut.CancelAuthorizationAsync(10, customerId: 5, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.OrderAccessDenied));
        await _paymentRepo.DidNotReceive().GetByOrderIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RefundAsync_HappyPath_PersistsRefundAndUpdatesOrderStatus()
    {
        Payment payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_r", Amount = 50m };
        Order order = new Order { Id = 10, PaymentStatus = PaymentStatus.Completed };
        _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepo.GetTotalRefundedAsync(1, Arg.Any<CancellationToken>()).Returns(0m);
        _stripe.CreateRefundAsync("pi_r", 2500, Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(new StripeRefundResult("re_1", "pi_r", 25m, "eur", "succeeded"));
        _paymentRepo.AddRefundAsync(Arg.Any<Refund>(), Arg.Any<CancellationToken>())
                    .Returns(ci => ci.Arg<Refund>());

        ServiceResult<RefundDto> result = await _sut.RefundAsync(10, 25m, "customer request", adminUserId: 99, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Amount, Is.EqualTo(25m));
        Assert.That(order.PaymentStatus, Is.EqualTo(PaymentStatus.PartiallyRefunded));
    }

    [Test]
    public async Task RefundAsync_FullRefund_SetsStatusRefunded()
    {
        Payment payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_r", Amount = 50m };
        Order order = new Order { Id = 10, PaymentStatus = PaymentStatus.Completed };
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
        Payment payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_r", Amount = 50m };
        _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
        _paymentRepo.GetTotalRefundedAsync(1, Arg.Any<CancellationToken>()).Returns(45m);

        ServiceResult<RefundDto> result = await _sut.RefundAsync(10, 10m, "x", adminUserId: 99, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Does.Contain("dépasse"));
        await _stripe.DidNotReceive().CreateRefundAsync(
            Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RefundAsync_WithOpenDispute_ReturnsBlockedError()
    {
        Payment payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_d", Amount = 50m };
        _paymentRepo.GetByOrderIdAsync(10, Arg.Any<CancellationToken>()).Returns(payment);
        _disputeService.HasOpenDisputeForOrderAsync(10, Arg.Any<CancellationToken>()).Returns(true);

        ServiceResult<RefundDto> result = await _sut.RefundAsync(10, 10m, "test", adminUserId: 99, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.RefundBlockedByOpenDispute));
        await _stripe.DidNotReceive().CreateRefundAsync(
            Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleStripeEventAsync_DuplicateEvent_ReturnsSuccessWithoutWork()
    {
        Stripe.Event evt = new Stripe.Event { Id = "evt_dup", Type = "payment_intent.succeeded" };
        _paymentRepo.TryRegisterProcessedEventAsync("evt_dup", "payment_intent.succeeded", Arg.Any<CancellationToken>())
                    .Returns(false);

        ServiceResult result = await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _paymentRepo.DidNotReceive().GetByStripePaymentIntentIdAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleStripeEventAsync_AmountCapturableUpdated_TransitionsOrderAndCommitsRedemptions()
    {
        Payment payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_x", Status = PaymentGatewayStatus.RequiresConfirmation };
        Order order = new Order { Id = 10, Status = OrderStatus.AwaitingPayment, PaymentStatus = PaymentStatus.Pending, CustomerId = 2 };
        Stripe.PaymentIntent pi = new Stripe.PaymentIntent { Id = "pi_x" };
        Stripe.Event evt = new Stripe.Event { Id = "evt_1", Type = "payment_intent.amount_capturable_updated", Data = new Stripe.EventData { Object = pi } };
        _paymentRepo.TryRegisterProcessedEventAsync("evt_1", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _paymentRepo.GetByStripePaymentIntentIdAsync("pi_x", Arg.Any<CancellationToken>()).Returns(payment);
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _cartRepo.GetByCustomerAsync(2, Arg.Any<CancellationToken>()).Returns(new List<Cart>());

        ServiceResult result = await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(order.Status, Is.EqualTo(OrderStatus.Pending));
        Assert.That(order.PaymentStatus, Is.EqualTo(PaymentStatus.Authorized));
        Assert.That(payment.AuthorizedAt, Is.Not.Null);
    }

    [Test]
    public async Task HandleStripeEventAsync_PaymentFailed_CancelsOrderAndReverses()
    {
        Payment payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_f" };
        Order order = new Order { Id = 10, Status = OrderStatus.AwaitingPayment };
        Stripe.PaymentIntent pi = new Stripe.PaymentIntent { Id = "pi_f" };
        Stripe.Event evt = new Stripe.Event { Id = "evt_f", Type = "payment_intent.payment_failed", Data = new Stripe.EventData { Object = pi } };
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
        Payment payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_r", Amount = 100m };
        Order order = new Order { Id = 10, PaymentStatus = PaymentStatus.Completed };
        Stripe.Charge charge = new Stripe.Charge
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
        Stripe.Event evt = new Stripe.Event { Id = "evt_ref", Type = "charge.refunded", Data = new Stripe.EventData { Object = charge } };
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

    [Test]
    public async Task HandleStripeEventAsync_HandlerThrows_RollsBackProcessedEvent()
    {
        // Arrange: TryRegisterProcessedEventAsync returns true (event registered),
        // but the payment repo then throws during handler execution.
        // The exception should propagate so that Stripe can retry the webhook.
        // With real DB + transaction this means the ProcessedStripeEvent row is also rolled back.
        Stripe.PaymentIntent pi = new Stripe.PaymentIntent { Id = "pi_throw" };
        Stripe.Event evt = new Stripe.Event
        {
            Id = "evt_throw",
            Type = "payment_intent.succeeded",
            Data = new Stripe.EventData { Object = pi }
        };

        _paymentRepo.TryRegisterProcessedEventAsync("evt_throw", Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(true);
        _paymentRepo.GetByStripePaymentIntentIdAsync("pi_throw", Arg.Any<CancellationToken>())
                    .ThrowsAsync(new InvalidOperationException("handler blowup"));

        // The exception must propagate — not be swallowed — so Stripe can retry.
        Assert.That(
            async () => await _sut.HandleStripeEventAsync(evt, CancellationToken.None),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("handler blowup"));
    }

    [Test]
    public async Task HandleAuthorizationCompletedAsync_HandlerThrowsAfterPartialUpdates_RollsBackEverything()
    {
        // Arrange: payment and order exist; cart clearing throws after order status has been mutated.
        // Because all DB work runs inside BeginTransactionAsync, a real DB would roll everything back.
        // In this unit test (mocked repos) we verify that:
        //   - the exception propagates (Stripe can retry),
        //   - email queuing never fires (publishes are deferred until after commit).
        Payment payment = new Payment
        {
            Id = 1,
            OrderId = 10,
            StripePaymentIntentId = "pi_partial",
            Status = PaymentGatewayStatus.RequiresConfirmation
        };
        Order order = new Order { Id = 10, Status = OrderStatus.AwaitingPayment, CustomerId = 2 };
        Stripe.PaymentIntent pi = new Stripe.PaymentIntent { Id = "pi_partial" };
        Stripe.Event evt = new Stripe.Event
        {
            Id = "evt_partial",
            Type = "payment_intent.amount_capturable_updated",
            Data = new Stripe.EventData { Object = pi }
        };

        _paymentRepo.TryRegisterProcessedEventAsync("evt_partial", Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(true);
        _paymentRepo.GetByStripePaymentIntentIdAsync("pi_partial", Arg.Any<CancellationToken>())
                    .Returns(payment);
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>())
                  .Returns(order);
        // Loyalty and discount commit succeed, but cart clearing throws.
        _cartRepo.GetByCustomerAsync(2, Arg.Any<CancellationToken>())
                 .ThrowsAsync(new InvalidOperationException("cart boom"));

        // The exception must propagate so Stripe retries the webhook.
        Assert.That(
            async () => await _sut.HandleStripeEventAsync(evt, CancellationToken.None),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("cart boom"));

        // Email queuing must NOT have happened (publish deferred until after commit).
        await _emailJobService.DidNotReceive()
            .QueueOrderConfirmationAsync(Arg.Any<Order>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task HandleAuthorizationCompletedAsync_IncrementsDiscountCounter()
    {
        // When payment_intent.amount_capturable_updated is handled successfully,
        // IncrementRedemptionCountersForCommittedAsync must be called so that
        // CurrentRedemptions is only bumped at actual payment commit (not at order creation).
        Payment payment = new Payment
        {
            Id = 1,
            OrderId = 10,
            StripePaymentIntentId = "pi_dc",
            Status = PaymentGatewayStatus.RequiresConfirmation
        };
        Order order = new Order { Id = 10, Status = OrderStatus.AwaitingPayment, CustomerId = 3 };
        Stripe.PaymentIntent pi = new Stripe.PaymentIntent { Id = "pi_dc" };
        Stripe.Event evt = new Stripe.Event
        {
            Id = "evt_dc",
            Type = "payment_intent.amount_capturable_updated",
            Data = new Stripe.EventData { Object = pi }
        };

        _paymentRepo.TryRegisterProcessedEventAsync("evt_dc", Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(true);
        _paymentRepo.GetByStripePaymentIntentIdAsync("pi_dc", Arg.Any<CancellationToken>())
                    .Returns(payment);
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>())
                  .Returns(order);
        _cartRepo.GetByCustomerAsync(3, Arg.Any<CancellationToken>())
                 .Returns(new List<Cart>());

        ServiceResult result = await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _discountRepo.Received(1).IncrementRedemptionCountersForCommittedAsync(10, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleStripeEventAsync_UnknownEventType_ReturnsSuccess()
    {
        // Unknown event types must be acknowledged (return 200) and logged (not asserted here).
        Stripe.Event evt = new Stripe.Event { Id = "evt_unknown", Type = "some.unknown.event" };
        _paymentRepo.TryRegisterProcessedEventAsync("evt_unknown", Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(true);

        ServiceResult result = await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        // No handler should have been invoked — no payment lookup.
        await _paymentRepo.DidNotReceive()
            .GetByStripePaymentIntentIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAuthorizationCompletedAsync_QueuesInvoiceCreation()
    {
        Payment payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_x" };
        Order order = new Order { Id = 10, Status = OrderStatus.AwaitingPayment, PaymentStatus = PaymentStatus.Pending, CustomerId = 2 };
        Stripe.PaymentIntent pi = new Stripe.PaymentIntent { Id = "pi_x" };
        Stripe.Event evt = new Stripe.Event { Id = "evt_auth", Type = "payment_intent.amount_capturable_updated", Data = new Stripe.EventData { Object = pi } };

        _paymentRepo.TryRegisterProcessedEventAsync("evt_auth", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _paymentRepo.GetByStripePaymentIntentIdAsync("pi_x", Arg.Any<CancellationToken>()).Returns(payment);
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _cartRepo.GetByCustomerAsync(2, Arg.Any<CancellationToken>()).Returns(new List<Cart>());
        // GetByIdWithFullDetailsAsync needed for email queue path
        _orderRepo.GetByIdWithFullDetailsAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _invoiceService.CreatePendingInvoicesForCapturedOrderAsync(10, Arg.Any<CancellationToken>())
                       .Returns(ServiceResult<List<InvoiceJobMessage>>.Success(new List<InvoiceJobMessage> { new(100), new(101) }));

        await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

        await _invoiceService.Received(1).CreatePendingInvoicesForCapturedOrderAsync(10, Arg.Any<CancellationToken>());
        // Verify the two invoice-queue publishes were issued after commit
        await _publisher.Received(1).PublishAsync("invoice", Arg.Is<InvoiceJobMessage>(m => m.InvoiceId == 100), Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync("invoice", Arg.Is<InvoiceJobMessage>(m => m.InvoiceId == 101), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleChargeRefundedAsync_QueuesCreditNoteCreation()
    {
        Payment payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_r", Amount = 100m };
        Order order = new Order { Id = 10, PaymentStatus = PaymentStatus.Completed };
        Stripe.Charge charge = new Stripe.Charge
        {
            Id = "ch_1",
            PaymentIntentId = "pi_r",
            Refunds = new Stripe.StripeList<Stripe.Refund>
            {
                Data = new List<Stripe.Refund>
                {
                    new() { Id = "re_new", Amount = 2500, Currency = "eur", Status = "succeeded" },
                },
            },
        };
        Stripe.Event evt = new Stripe.Event { Id = "evt_ref2", Type = "charge.refunded", Data = new Stripe.EventData { Object = charge } };

        _paymentRepo.TryRegisterProcessedEventAsync("evt_ref2", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _paymentRepo.GetByStripePaymentIntentIdAsync("pi_r", Arg.Any<CancellationToken>()).Returns(payment);
        _paymentRepo.GetRefundByStripeIdAsync("re_new", Arg.Any<CancellationToken>()).Returns((Refund?)null);
        _paymentRepo.AddRefundAsync(Arg.Any<Refund>(), Arg.Any<CancellationToken>()).Returns(ci => { Refund r = ci.Arg<Refund>(); r.Id = 555; return r; });
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepo.GetTotalRefundedAsync(1, Arg.Any<CancellationToken>()).Returns(25m);
        _invoiceService.CreateCreditNotesForRefundAsync(555, Arg.Any<CancellationToken>())
                       .Returns(ServiceResult<List<InvoiceJobMessage>>.Success(new List<InvoiceJobMessage> { new(200), new(201) }));

        await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

        await _invoiceService.Received(1).CreateCreditNotesForRefundAsync(555, Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync("invoice", Arg.Is<InvoiceJobMessage>(m => m.InvoiceId == 200), Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync("invoice", Arg.Is<InvoiceJobMessage>(m => m.InvoiceId == 201), Arg.Any<CancellationToken>());
    }

    #region Dispute webhook dispatch

    [Test]
    public async Task HandleStripeEventAsync_ChargeDisputeCreated_DispatchesToDisputeService()
    {
        Stripe.Dispute stripeDispute = new Stripe.Dispute
        {
            Id = "dp_1",
            ChargeId = "ch_1",
            Amount = 1000,
            Currency = "eur",
            Reason = "fraudulent",
            Created = DateTime.UtcNow,
        };
        Stripe.Event evt = new Stripe.Event
        {
            Id = "evt_disp_c",
            Type = "charge.dispute.created",
            Data = new Stripe.EventData { Object = stripeDispute },
        };
        _paymentRepo.TryRegisterProcessedEventAsync("evt_disp_c", "charge.dispute.created", Arg.Any<CancellationToken>())
            .Returns(true);
        _disputeService.HandleCreatedAsync(stripeDispute, Arg.Any<List<Func<Task>>>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Dispute>.Success(new Dispute { Id = 1 }));

        ServiceResult result = await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _disputeService.Received(1).HandleCreatedAsync(
            stripeDispute, Arg.Any<List<Func<Task>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleStripeEventAsync_ChargeDisputeUpdated_DispatchesToHandleUpdated()
    {
        Stripe.Dispute stripeDispute = new Stripe.Dispute { Id = "dp_2" };
        Stripe.Event evt = new Stripe.Event
        {
            Id = "evt_disp_u",
            Type = "charge.dispute.updated",
            Data = new Stripe.EventData { Object = stripeDispute },
        };
        _paymentRepo.TryRegisterProcessedEventAsync("evt_disp_u", "charge.dispute.updated", Arg.Any<CancellationToken>())
            .Returns(true);
        _disputeService.HandleUpdatedAsync(stripeDispute, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

        await _disputeService.Received(1).HandleUpdatedAsync(stripeDispute, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleStripeEventAsync_ChargeDisputeClosed_DispatchesToHandleClosed()
    {
        Stripe.Dispute stripeDispute = new Stripe.Dispute { Id = "dp_3", Status = "won" };
        Stripe.Event evt = new Stripe.Event
        {
            Id = "evt_disp_cl",
            Type = "charge.dispute.closed",
            Data = new Stripe.EventData { Object = stripeDispute },
        };
        _paymentRepo.TryRegisterProcessedEventAsync("evt_disp_cl", "charge.dispute.closed", Arg.Any<CancellationToken>())
            .Returns(true);
        _disputeService.HandleClosedAsync(stripeDispute, Arg.Any<List<Func<Task>>>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

        await _disputeService.Received(1).HandleClosedAsync(
            stripeDispute, Arg.Any<List<Func<Task>>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleStripeEventAsync_ChargeDisputeWarning_AcksWithoutDispatch()
    {
        Stripe.Event evt = new Stripe.Event
        {
            Id = "evt_w",
            Type = "charge.dispute.warning_needs_response",
            Data = new Stripe.EventData { Object = new Stripe.Dispute() },
        };
        _paymentRepo.TryRegisterProcessedEventAsync("evt_w", "charge.dispute.warning_needs_response", Arg.Any<CancellationToken>())
            .Returns(true);

        ServiceResult result = await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _disputeService.DidNotReceive().HandleCreatedAsync(
            Arg.Any<Stripe.Dispute>(), Arg.Any<List<Func<Task>>>(), Arg.Any<CancellationToken>());
    }

    #endregion

    // ─── Commission statement refund wiring ───────────────────────────────

    [Test]
    public async Task HandleChargeRefundedAsync_CallsCommissionStatementHandlerForNewRefund()
    {
        Payment payment = new Payment { Id = 1, OrderId = 10, StripePaymentIntentId = "pi_cs", Amount = 100m };
        Order order = new Order { Id = 10, PaymentStatus = PaymentStatus.Completed };
        Refund refund = new Refund { Id = 77, PaymentId = 1, StripeRefundId = "re_abc", Amount = 30m, Currency = "EUR" };

        Stripe.Charge charge = new Stripe.Charge
        {
            Id = "ch_cs",
            PaymentIntentId = "pi_cs",
            Refunds = new Stripe.StripeList<Stripe.Refund>
            {
                Data = new List<Stripe.Refund>
                {
                    new() { Id = "re_abc", Amount = 3000, Currency = "eur", Status = "succeeded" },
                },
            },
        };
        Stripe.Event evt = new Stripe.Event
        {
            Id = "evt_cs",
            Type = "charge.refunded",
            Data = new Stripe.EventData { Object = charge },
        };

        _paymentRepo.TryRegisterProcessedEventAsync("evt_cs", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _paymentRepo.GetByStripePaymentIntentIdAsync("pi_cs", Arg.Any<CancellationToken>())
            .Returns(payment);
        _paymentRepo.GetRefundByStripeIdAsync("re_abc", Arg.Any<CancellationToken>())
            .Returns((Refund?)null);
        _paymentRepo.AddRefundAsync(Arg.Any<Refund>(), Arg.Any<CancellationToken>())
            .Returns(ci => { Refund r = ci.Arg<Refund>(); r.Id = 77; return r; });
        _paymentRepo.GetTotalRefundedAsync(1, Arg.Any<CancellationToken>())
            .Returns(30m);
        _paymentRepo.GetRefundByIdAsync(77, Arg.Any<CancellationToken>())
            .Returns(refund);
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(order);
        _commissionStatementService
            .HandleRefundForPriorPeriodAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        await _sut.HandleStripeEventAsync(evt, CancellationToken.None);

        await _commissionStatementService.Received(1).HandleRefundForPriorPeriodAsync(
            10, 77, "re_abc", 30m, Arg.Any<CancellationToken>());
    }
}
