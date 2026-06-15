using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Factories;
using DeliverTableTests.Global.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class DisputeServiceTests
{
    private IDisputeRepository _disputeRepo = null!;
    private IPaymentRepository _paymentRepo = null!;
    private IOrderRepository _orderRepo = null!;
    private IRestaurantRepository _restaurantRepo = null!;
    private IRestaurantTransactionRepository _txnRepo = null!;
    private IEmailJobRepository _emailJobRepo = null!;
    private IAdminNotificationService _notifications = null!;
    private IMessagePublisher _publisher = null!;
    private AppEnvironment _env = null!;
    private DisputeService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _disputeRepo = Substitute.For<IDisputeRepository>();
        _paymentRepo = Substitute.For<IPaymentRepository>();
        _orderRepo = Substitute.For<IOrderRepository>();
        _restaurantRepo = Substitute.For<IRestaurantRepository>();
        _txnRepo = Substitute.For<IRestaurantTransactionRepository>();
        _emailJobRepo = Substitute.For<IEmailJobRepository>();
        _notifications = Substitute.For<IAdminNotificationService>();
        _publisher = Substitute.For<IMessagePublisher>();
        _env = TestEnvironmentFactory.Create();
        _sut = new DisputeService(
            _disputeRepo, _paymentRepo, _orderRepo, _restaurantRepo, _txnRepo,
            _emailJobRepo, _notifications, _publisher, _env,
            NullLogger<DisputeService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        AppEnvironmentTestHelper.CleanupEnvironment();
    }

    private static Stripe.Dispute CreateStripeDispute(
        string id = "dp_1",
        string chargeId = "ch_1",
        long amount = 2500,
        string reason = "fraudulent",
        string? status = null,
        DateTime? dueBy = null)
    {
        return new Stripe.Dispute
        {
            Id = id,
            ChargeId = chargeId,
            Amount = amount,
            Currency = "eur",
            Reason = reason,
            Status = status,
            Created = DateTime.UtcNow,
            EvidenceDetails = new Stripe.DisputeEvidenceDetails
            {
                DueBy = dueBy ?? DateTime.UtcNow.AddDays(7),
            },
        };
    }

    #region HandleCreatedAsync

    [Test]
    public async Task HandleCreatedAsync_HappyPath_PersistsDisputeReversalAndDefersPublishes()
    {
        Stripe.Dispute stripeDispute = CreateStripeDispute();
        Payment payment = new Payment { Id = 1, StripeChargeId = "ch_1", OrderId = 10, Amount = 100m };
        Order order = new Order { Id = 10, RestaurantId = 5, CustomerId = 2 };
        User owner = new User { Id = 99, Email = "owner@rest.fr" };
        Restaurant restaurant = new Restaurant { Id = 5, Name = "Chez Toto", Balance = 100m, OwnerId = 99, Owner = owner };

        _disputeRepo.GetByStripeDisputeIdAsync("dp_1", Arg.Any<CancellationToken>()).Returns((Dispute?)null);
        _paymentRepo.GetByStripeChargeIdAsync("ch_1", Arg.Any<CancellationToken>()).Returns(payment);
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _restaurantRepo.GetByIdWithOwnerAsync(5, Arg.Any<CancellationToken>()).Returns(restaurant);
        _disputeRepo.CreateAsync(Arg.Any<Dispute>(), Arg.Any<CancellationToken>())
            .Returns(ci => { Dispute d = ci.Arg<Dispute>(); d.Id = 42; return d; });

        List<Func<Task>> deferred = new List<Func<Task>>();
        ServiceResult<Dispute> result = await _sut.HandleCreatedAsync(stripeDispute, deferred, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _disputeRepo.Received(1).CreateAsync(
            Arg.Is<Dispute>(d =>
                d.StripeDisputeId == "dp_1" &&
                d.Amount == 25m &&
                d.RestaurantId == 5 &&
                d.PaymentId == 1 &&
                d.OrderId == 10 &&
                d.State == DisputeState.Open),
            Arg.Any<CancellationToken>());
        await _txnRepo.Received(1).CreateAsync(
            Arg.Is<RestaurantTransaction>(t =>
                t.Type == TransactionType.DisputeReversal &&
                t.NetAmount == -25m &&
                t.BalanceAfter == 75m),
            Arg.Any<CancellationToken>());
        Assert.That(restaurant.Balance, Is.EqualTo(75m));
        await _notifications.Received(1).RaiseForAllAdminsAsync(
            NotificationType.Dispute, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _notifications.Received(1).RaiseForUserAsync(
            99, NotificationType.Dispute, Arg.Any<string>(), Arg.Any<CancellationToken>());
        // Two email jobs created (admin + restaurant) before commit.
        await _emailJobRepo.Received(2).CreateAsync(Arg.Any<EmailJob>(), Arg.Any<CancellationToken>());
        // Publishes are deferred, not yet executed.
        Assert.That(deferred, Has.Count.EqualTo(2));
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleCreatedAsync_DuplicateStripeDisputeId_SkipsIdempotently()
    {
        Dispute existing = new Dispute { Id = 42, StripeDisputeId = "dp_1", State = DisputeState.Open };
        _disputeRepo.GetByStripeDisputeIdAsync("dp_1", Arg.Any<CancellationToken>()).Returns(existing);

        List<Func<Task>> deferred = new List<Func<Task>>();
        ServiceResult<Dispute> result = await _sut.HandleCreatedAsync(CreateStripeDispute(), deferred, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _disputeRepo.DidNotReceive().CreateAsync(Arg.Any<Dispute>(), Arg.Any<CancellationToken>());
        await _txnRepo.DidNotReceive().CreateAsync(Arg.Any<RestaurantTransaction>(), Arg.Any<CancellationToken>());
        Assert.That(deferred, Is.Empty);
    }

    [Test]
    public async Task HandleCreatedAsync_PaymentNotFound_ReturnsError()
    {
        _disputeRepo.GetByStripeDisputeIdAsync("dp_1", Arg.Any<CancellationToken>()).Returns((Dispute?)null);
        _paymentRepo.GetByStripeChargeIdAsync("ch_missing", Arg.Any<CancellationToken>()).Returns((Payment?)null);

        ServiceResult<Dispute> result = await _sut.HandleCreatedAsync(
            CreateStripeDispute(chargeId: "ch_missing"),
            new List<Func<Task>>(),
            CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.DisputePaymentNotFound));
    }

    [Test]
    public async Task HandleCreatedAsync_PartialAmount_ReversesOnlyDisputedAmount()
    {
        Stripe.Dispute stripeDispute = CreateStripeDispute(amount: 1500); // 15 EUR
        Payment payment = new Payment { Id = 1, StripeChargeId = "ch_1", OrderId = 10, Amount = 100m };
        Order order = new Order { Id = 10, RestaurantId = 5 };
        Restaurant restaurant = new Restaurant { Id = 5, Balance = 200m, OwnerId = 99 };
        _disputeRepo.GetByStripeDisputeIdAsync("dp_1", Arg.Any<CancellationToken>()).Returns((Dispute?)null);
        _paymentRepo.GetByStripeChargeIdAsync("ch_1", Arg.Any<CancellationToken>()).Returns(payment);
        _orderRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _restaurantRepo.GetByIdWithOwnerAsync(5, Arg.Any<CancellationToken>()).Returns(restaurant);

        await _sut.HandleCreatedAsync(stripeDispute, new List<Func<Task>>(), CancellationToken.None);

        await _txnRepo.Received(1).CreateAsync(
            Arg.Is<RestaurantTransaction>(t => t.NetAmount == -15m && t.BalanceAfter == 185m),
            Arg.Any<CancellationToken>());
        Assert.That(restaurant.Balance, Is.EqualTo(185m));
    }

    #endregion

    #region HandleUpdatedAsync

    [Test]
    public async Task HandleUpdatedAsync_RefreshesDueByAndPayload()
    {
        Dispute existing = new Dispute
        {
            Id = 42,
            StripeDisputeId = "dp_1",
            State = DisputeState.Open,
            DueBy = DateTime.UtcNow,
            StripePayload = "{}",
        };
        _disputeRepo.GetByStripeDisputeIdAsync("dp_1", Arg.Any<CancellationToken>()).Returns(existing);
        DateTime newDueBy = DateTime.UtcNow.AddDays(14);
        Stripe.Dispute stripeDispute = CreateStripeDispute(dueBy: newDueBy);

        ServiceResult result = await _sut.HandleUpdatedAsync(stripeDispute, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(existing.DueBy, Is.EqualTo(newDueBy));
        await _disputeRepo.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleUpdatedAsync_MissingDispute_ReturnsError()
    {
        _disputeRepo.GetByStripeDisputeIdAsync("dp_X", Arg.Any<CancellationToken>()).Returns((Dispute?)null);
        Stripe.Dispute stripeDispute = CreateStripeDispute(id: "dp_X");

        ServiceResult result = await _sut.HandleUpdatedAsync(stripeDispute, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.DisputeNotFound));
    }

    #endregion

    #region HandleClosedAsync

    [Test]
    public async Task HandleClosedAsync_Won_CreatesRestoreAndRestoresBalance()
    {
        Dispute dispute = new Dispute
        {
            Id = 42,
            StripeDisputeId = "dp_1",
            OrderId = 10,
            RestaurantId = 5,
            Amount = 25m,
            State = DisputeState.Open,
        };
        User owner = new User { Id = 99, Email = "o@r.fr" };
        Restaurant restaurant = new Restaurant { Id = 5, Name = "Chez Toto", Balance = 75m, OwnerId = 99, Owner = owner };
        _disputeRepo.GetByStripeDisputeIdAsync("dp_1", Arg.Any<CancellationToken>()).Returns(dispute);
        _restaurantRepo.GetByIdWithOwnerAsync(5, Arg.Any<CancellationToken>()).Returns(restaurant);

        List<Func<Task>> deferred = new List<Func<Task>>();
        ServiceResult result = await _sut.HandleClosedAsync(
            CreateStripeDispute(status: "won"), deferred, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(dispute.State, Is.EqualTo(DisputeState.Won));
        Assert.That(dispute.ClosedAt, Is.Not.Null);
        await _txnRepo.Received(1).CreateAsync(
            Arg.Is<RestaurantTransaction>(t =>
                t.Type == TransactionType.DisputeRestored &&
                t.NetAmount == 25m &&
                t.BalanceAfter == 100m),
            Arg.Any<CancellationToken>());
        Assert.That(restaurant.Balance, Is.EqualTo(100m));
        Assert.That(deferred, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task HandleClosedAsync_Lost_ClosesWithoutBalanceChange()
    {
        Dispute dispute = new Dispute
        {
            Id = 42,
            StripeDisputeId = "dp_1",
            OrderId = 10,
            RestaurantId = 5,
            Amount = 25m,
            State = DisputeState.Open,
        };
        User owner = new User { Id = 99, Email = "o@r.fr" };
        Restaurant restaurant = new Restaurant { Id = 5, Name = "Chez Toto", Balance = 75m, OwnerId = 99, Owner = owner };
        _disputeRepo.GetByStripeDisputeIdAsync("dp_1", Arg.Any<CancellationToken>()).Returns(dispute);
        _restaurantRepo.GetByIdWithOwnerAsync(5, Arg.Any<CancellationToken>()).Returns(restaurant);

        ServiceResult result = await _sut.HandleClosedAsync(
            CreateStripeDispute(status: "lost"), new List<Func<Task>>(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(dispute.State, Is.EqualTo(DisputeState.Lost));
        Assert.That(restaurant.Balance, Is.EqualTo(75m));
        await _txnRepo.DidNotReceive().CreateAsync(Arg.Any<RestaurantTransaction>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleClosedAsync_AlreadyClosed_IsIdempotent()
    {
        Dispute dispute = new Dispute
        {
            Id = 42,
            StripeDisputeId = "dp_1",
            OrderId = 10,
            RestaurantId = 5,
            Amount = 25m,
            State = DisputeState.Won,
        };
        _disputeRepo.GetByStripeDisputeIdAsync("dp_1", Arg.Any<CancellationToken>()).Returns(dispute);

        ServiceResult result = await _sut.HandleClosedAsync(
            CreateStripeDispute(status: "won"), new List<Func<Task>>(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await _txnRepo.DidNotReceive().CreateAsync(Arg.Any<RestaurantTransaction>(), Arg.Any<CancellationToken>());
        await _disputeRepo.DidNotReceive().UpdateAsync(Arg.Any<Dispute>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleClosedAsync_MissingDispute_ReturnsError()
    {
        _disputeRepo.GetByStripeDisputeIdAsync("dp_X", Arg.Any<CancellationToken>()).Returns((Dispute?)null);
        Stripe.Dispute stripeDispute = CreateStripeDispute(id: "dp_X", status: "won");

        ServiceResult result = await _sut.HandleClosedAsync(stripeDispute, new List<Func<Task>>(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.DisputeNotFound));
    }

    #endregion

    #region HasOpenDisputeForOrderAsync

    [Test]
    public async Task HasOpenDisputeForOrderAsync_DelegatesToRepository()
    {
        _disputeRepo.HasOpenForOrderAsync(10, Arg.Any<CancellationToken>()).Returns(true);

        bool result = await _sut.HasOpenDisputeForOrderAsync(10, CancellationToken.None);

        Assert.That(result, Is.True);
    }

    #endregion
}
