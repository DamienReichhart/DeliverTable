using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class CommissionStatementServiceTests
{
    private ICommissionStatementRepository _repo = null!;
    private IRestaurantRepository _restaurantRepo = null!;
    private IMessagePublisher _publisher = null!;
    private CommissionStatementService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ICommissionStatementRepository>();
        _restaurantRepo = Substitute.For<IRestaurantRepository>();
        _publisher = Substitute.For<IMessagePublisher>();

        var env = AppEnvironmentTestHelper.SetupEnvironment();

        _sut = new CommissionStatementService(
            _repo,
            _restaurantRepo,
            _publisher,
            env,
            NullLogger<CommissionStatementService>.Instance);
    }

    [TearDown]
    public void TearDown() => AppEnvironmentTestHelper.CleanupEnvironment();

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
}
