using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Configuration;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Helpers;
using DeliverTableTests.Server.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class CommissionStatementServiceTests
{
    private ICommissionStatementRepository _repo = null!;
    private IRestaurantRepository _restaurantRepo = null!;
    private IOrderRepository _orderRepo = null!;
    private IMessagePublisher _publisher = null!;
    private CommissionStatementService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ICommissionStatementRepository>();
        _restaurantRepo = Substitute.For<IRestaurantRepository>();
        _orderRepo = Substitute.For<IOrderRepository>();
        _publisher = Substitute.For<IMessagePublisher>();

        var env = AppEnvironmentTestHelper.SetupEnvironment();

        _sut = new CommissionStatementService(
            _repo,
            _restaurantRepo,
            _orderRepo,
            _publisher,
            env,
            NullLogger<CommissionStatementService>.Instance);
    }

    [TearDown]
    public void TearDown() => AppEnvironmentTestHelper.CleanupEnvironment();

    // ── GenerateForPeriodAsync tests ─────────────────────────────────────────

    [Test]
    public async Task GenerateForPeriodAsync_HappyPath_CreatesStatementAndPublishesMessage()
    {
        // Arrange
        const int restaurantId = 7;
        var restaurant = BuildRestaurant(restaurantId);
        var order = BuildDeliveredOrder(restaurantId, 100m, new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc));

        _repo.ListRestaurantIdsWithEligibleOrdersAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([restaurantId]);

        _repo.InvoiceExistsForPeriodAsync(restaurantId, 2026, 5, Arg.Any<CancellationToken>())
            .Returns(false);

        _restaurantRepo.GetByIdWithOwnerAsync(restaurantId, Arg.Any<CancellationToken>())
            .Returns(restaurant);

        _repo.ListEligibleOrdersForRestaurantAsync(
                restaurantId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([order]);

        _repo.AllocateNextNumberAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        _repo.CreateAsync(Arg.Any<CommissionStatement>(), Arg.Any<CancellationToken>())
            .Returns(x => x.Arg<CommissionStatement>());

        // Act
        var result = await _sut.GenerateForPeriodAsync(2026, 5, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);

        var dto = result.Value!;
        Assert.That(dto.StatementsCreated, Is.EqualTo(1));

        await _repo.Received(1).CreateAsync(
            Arg.Is<CommissionStatement>(s =>
                s.Lines.Count == 1
                && s.Lines[0].LineHt == 10m
                && s.Kind == CommissionStatementKind.Invoice
                && s.PeriodYear == 2026
                && s.PeriodMonth == 5
                && s.RecipientRestaurantId == restaurantId),
            Arg.Any<CancellationToken>());

        await _publisher.Received(1).PublishAsync(
            MessagingExchanges.CommissionStatement,
            Arg.Any<CommissionStatementJobMessage>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GenerateForPeriodAsync_SkipsRestaurant_WhenNoEligibleOrders()
    {
        _repo.ListRestaurantIdsWithEligibleOrdersAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<int> { 7 });
        _repo.InvoiceExistsForPeriodAsync(7, 2026, 5, Arg.Any<CancellationToken>()).Returns(false);
        _restaurantRepo.GetByIdWithOwnerAsync(7, Arg.Any<CancellationToken>()).Returns(BuildRestaurant(7));
        _repo.ListEligibleOrdersForRestaurantAsync(7, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<Order>());

        var r = await _sut.GenerateForPeriodAsync(2026, 5, default);

        Assert.That(r.Value!.StatementsCreated, Is.EqualTo(0));
        Assert.That(r.Value.RestaurantsSkipped, Is.EqualTo(1));
        await _repo.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task GenerateForPeriodAsync_SkipsRestaurant_WhenInvoiceAlreadyExists()
    {
        _repo.ListRestaurantIdsWithEligibleOrdersAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<int> { 7 });
        _repo.InvoiceExistsForPeriodAsync(7, 2026, 5, Arg.Any<CancellationToken>()).Returns(true);

        var r = await _sut.GenerateForPeriodAsync(2026, 5, default);

        Assert.That(r.Value!.RestaurantsSkipped, Is.EqualTo(1));
        await _repo.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task GenerateForPeriodAsync_AppliesCommissionOnNetAmount_WhenPartiallyRefunded()
    {
        _repo.ListRestaurantIdsWithEligibleOrdersAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<int> { 7 });
        _repo.InvoiceExistsForPeriodAsync(7, 2026, 5, Arg.Any<CancellationToken>()).Returns(false);
        _restaurantRepo.GetByIdWithOwnerAsync(7, Arg.Any<CancellationToken>()).Returns(BuildRestaurant(7));

        var order = BuildDeliveredOrder(7, 100m, new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc));
        order.Payments.Add(BuildPaymentWithRefund(amount: 100m, refund: 30m));

        _repo.ListEligibleOrdersForRestaurantAsync(7, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<Order> { order });
        _repo.AllocateNextNumberAsync(Arg.Any<CancellationToken>()).Returns(1);
        _repo.CreateAsync(Arg.Any<CommissionStatement>(), Arg.Any<CancellationToken>())
            .Returns(x => x.Arg<CommissionStatement>());

        await _sut.GenerateForPeriodAsync(2026, 5, default);

        await _repo.Received(1).CreateAsync(Arg.Is<CommissionStatement>(s =>
            s.Lines.Count == 1
            && s.Lines[0].OrderTotalAmount == 70m
            && s.Lines[0].LineHt == 7m), default);
    }

    [Test]
    public async Task GenerateForPeriodAsync_ReturnsBadRequest_WhenInvalidMonth()
    {
        var r = await _sut.GenerateForPeriodAsync(2026, 13, default);
        Assert.That(r.IsSuccess, Is.False);
        Assert.That(r.Error!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task GenerateForPeriodAsync_SnapshotsCommissionRate_OnEachLine()
    {
        // Use 0.15 rate and no VAT for this test — set vars AFTER the base SetupEnvironment
        var env = AppEnvironmentTestHelper.SetupEnvironment();
        Environment.SetEnvironmentVariable("PLATFORM_COMMISSION_RATE", "0.15");
        Environment.SetEnvironmentVariable("PLATFORM_VAT_APPLICABLE", "false");
        env = AppEnvironment.Load();
        var sut = new CommissionStatementService(
            _repo, _restaurantRepo, _orderRepo, _publisher, env,
            NullLogger<CommissionStatementService>.Instance);

        _repo.ListRestaurantIdsWithEligibleOrdersAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<int> { 7 });
        _repo.InvoiceExistsForPeriodAsync(7, 2026, 5, Arg.Any<CancellationToken>()).Returns(false);
        _restaurantRepo.GetByIdWithOwnerAsync(7, Arg.Any<CancellationToken>()).Returns(BuildRestaurant(7));
        _repo.ListEligibleOrdersForRestaurantAsync(7, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<Order> { BuildDeliveredOrder(7, 100m, new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc)) });
        _repo.AllocateNextNumberAsync(Arg.Any<CancellationToken>()).Returns(1);
        _repo.CreateAsync(Arg.Any<CommissionStatement>(), Arg.Any<CancellationToken>())
            .Returns(x => x.Arg<CommissionStatement>());

        await sut.GenerateForPeriodAsync(2026, 5, default);

        await _repo.Received(1).CreateAsync(Arg.Is<CommissionStatement>(s =>
            s.Lines.All(l => l.CommissionRateSnapshot == 0.15m)
            && s.Lines.All(l => l.VatRate == 0m)), default);
    }

    // ── HandleRefundForPriorPeriodAsync tests ────────────────────────────────

    [Test]
    public async Task HandleRefundForPriorPeriod_NoOp_WhenOrderHasNoStatement()
    {
        var order = BuildDeliveredOrder(7, 100m, new DateTime(2026, 5, 10));
        order.CommissionStatementId = null;
        _orderRepo.GetByIdAsync(order.Id, default).Returns(order);

        var r = await _sut.HandleRefundForPriorPeriodAsync(order.Id, refundId: 99, "re_x", 30m, default);

        Assert.That(r.IsSuccess, Is.True);
        await _repo.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task HandleRefundForPriorPeriod_NoOp_WhenRefundEventAlreadyProcessed()
    {
        var order = BuildDeliveredOrder(7, 100m, new DateTime(2026, 5, 10));
        order.CommissionStatementId = 42;
        _orderRepo.GetByIdAsync(order.Id, default).Returns(order);
        _repo.FindLineByRefundEventIdAsync("re_x", default).Returns(new CommissionStatementLine());

        var r = await _sut.HandleRefundForPriorPeriodAsync(order.Id, refundId: 99, "re_x", 30m, default);

        Assert.That(r.IsSuccess, Is.True);
        await _repo.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task HandleRefundForPriorPeriod_CreatesCreditNote_UsingSnapshottedRate()
    {
        var order = BuildDeliveredOrder(7, 100m, new DateTime(2026, 5, 10));
        order.CommissionStatementId = 42;

        var originalStatement = CommissionStatementFactory.CreateInvoice(7, 2026, 5);
        originalStatement.Id = 42;
        originalStatement.Lines.Add(new CommissionStatementLine
        {
            OrderId = order.Id,
            CommissionRateSnapshot = 0.20m,
            VatRate = 20m,
        });

        _orderRepo.GetByIdAsync(order.Id, default).Returns(order);
        _repo.FindLineByRefundEventIdAsync("re_x", default).Returns((CommissionStatementLine?)null);
        _repo.GetByIdWithLinesAndRecipientAsync(42, default).Returns(originalStatement);
        _restaurantRepo.GetByIdWithOwnerAsync(7, Arg.Any<CancellationToken>()).Returns(BuildRestaurant(7));
        _repo.AllocateNextNumberAsync(Arg.Any<CancellationToken>()).Returns(99);
        _repo.CreateAsync(Arg.Any<CommissionStatement>(), Arg.Any<CancellationToken>())
            .Returns(x => x.Arg<CommissionStatement>());

        // env is 0.10m / 20% — credit note must use snapshotted 0.20 / 20%
        var r = await _sut.HandleRefundForPriorPeriodAsync(order.Id, refundId: 1, "re_x", refundedAmount: 30m, default);

        Assert.That(r.IsSuccess, Is.True);
        await _repo.Received(1).CreateAsync(Arg.Is<CommissionStatement>(s =>
            s.Kind == CommissionStatementKind.CreditNote
            && s.RelatedStatementId == 42
            && s.PeriodYear == 2026
            && s.PeriodMonth == 5
            && s.Lines.Count == 1
            && s.Lines[0].RefundEventId == "re_x"
            && s.Lines[0].LineHt == -6m       // 30 * 0.20
            && s.Lines[0].LineTtc == -7.20m), default);
        await _publisher.Received(1).PublishAsync(
            MessagingExchanges.CommissionStatement,
            Arg.Any<CommissionStatementJobMessage>(),
            Arg.Any<CancellationToken>());
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static Restaurant BuildRestaurant(int id)
    {
        var restaurant = CreateRestaurant(id: id, ownerId: 10);
        restaurant.LegalName = "Restaurant SAS";
        restaurant.LegalForm = "SAS";
        restaurant.Siret = "73282932000074";
        restaurant.LegalAddress = "1 rue Test, 75001 Paris";
        restaurant.Owner = CreateValidUser("owner@restaurant.fr");
        return restaurant;
    }

    private static Order BuildDeliveredOrder(int restaurantId, decimal total, DateTime deliveredAt) => new()
    {
        Id = 1,
        RestaurantId = restaurantId,
        Status = OrderStatus.Delivered,
        PaymentStatus = PaymentStatus.Completed,
        TotalAmount = total,
        OriginalAmount = total,
        DeliveredAt = deliveredAt,
        Payments = [],
        Items = [],
        Discounts = [],
    };

    private static Payment BuildPaymentWithRefund(decimal amount, decimal refund) => new()
    {
        Amount = amount,
        Currency = "EUR",
        Refunds =
        [
            new Refund { Amount = refund, StripeRefundId = "re_test", Currency = "EUR" }
        ],
    };
}
