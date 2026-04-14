using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Configuration;
using DeliverTableServer.Services;
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
    private AppEnvironment _env = null!;
    private PaymentService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _stripe = Substitute.For<IStripeGateway>();
        _paymentRepo = Substitute.For<IPaymentRepository>();
        _orderRepo = Substitute.For<IOrderRepository>();
        _userRepo = Substitute.For<IUserRepository>();
        _env = TestEnvironmentFactory.Create();
        _sut = new PaymentService(_stripe, _paymentRepo, _orderRepo, _userRepo, _env);
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
}
