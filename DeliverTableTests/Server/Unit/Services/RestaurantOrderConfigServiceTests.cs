using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Admin;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class RestaurantOrderConfigServiceTests
{
    private IOrderConfigRepository _orderConfigRepository = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private IOrderRepository _orderRepository = null!;
    private RestaurantOrderConfigService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _orderConfigRepository = Substitute.For<IOrderConfigRepository>();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _orderRepository = Substitute.For<IOrderRepository>();
        _sut = new RestaurantOrderConfigService(_orderConfigRepository, _restaurantRepository, _orderRepository);
    }

    [Test]
    public async Task GetTablesCapacityAsync_WhenOwnerDoesNotMatch_ReturnsCapacity()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 10);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _orderConfigRepository.GetRuleByRestaurantIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new OrderRule
            {
                RestaurantId = 1,
                TablesCapacityPerSlot = 3
            });
        _restaurantRepository.CountActiveTablesByMaxCapacityAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(4);

        var result = await _sut.GetTablesCapacityAsync(1, 999);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.RestaurantId, Is.EqualTo(1));
        Assert.That(result.Value.CapacityPerSlot, Is.EqualTo(3));
        Assert.That(result.Value.ActiveTablesFallback, Is.EqualTo(4));
    }

    [Test]
    public async Task GetBlockedSlotsAsync_WhenOwnerMatches_ReturnsSlots()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 10);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        _orderConfigRepository.GetBlockedSlotsByRestaurantAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<OrderBlockedSlot>
            {
                new()
                {
                    Id = 4,
                    RestaurantId = 1,
                    Restaurant = restaurant,
                    StartsAt = DateTime.UtcNow.AddDays(1),
                    EndsAt = DateTime.UtcNow.AddDays(1).AddHours(1),
                    Reason = "Privatisation"
                }
            });

        var result = await _sut.GetBlockedSlotsAsync(1, 10);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CreateBlockedSlotAsync_WhenDatesInvalid_ReturnsError()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 10);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        var request = new AdminCreateBlockedSlotRequest
        {
            StartsAt = DateTime.UtcNow.AddHours(5),
            EndsAt = DateTime.UtcNow.AddHours(4)
        };

        var result = await _sut.CreateBlockedSlotAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.InvalidBlockedSlotDates));
    }

    [Test]
    public async Task CreateBlockedSlotAsync_WhenOverlapExists_ReturnsError()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 10);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _orderConfigRepository.ExistsBlockedSlotOverlapAsync(
                1,
                Arg.Any<int?>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var request = new AdminCreateBlockedSlotRequest
        {
            StartsAt = DateTime.UtcNow.AddHours(4),
            EndsAt = DateTime.UtcNow.AddHours(6),
            Reason = "Maintenance"
        };

        var result = await _sut.CreateBlockedSlotAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.BlockedSlotOverlapExists));
    }

    [Test]
    public async Task CreateBlockedSlotAsync_WhenValid_CreatesSlot()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 10);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _orderConfigRepository.ExistsBlockedSlotOverlapAsync(
                1,
                Arg.Any<int?>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        _orderConfigRepository.CreateBlockedSlotAsync(Arg.Any<OrderBlockedSlot>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var slot = callInfo.Arg<OrderBlockedSlot>();
                slot.Id = 11;
                slot.Restaurant = restaurant;
                return slot;
            });

        var request = new AdminCreateBlockedSlotRequest
        {
            StartsAt = DateTime.UtcNow.AddHours(4),
            EndsAt = DateTime.UtcNow.AddHours(6),
            Reason = "Maintenance"
        };

        var result = await _sut.CreateBlockedSlotAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Id, Is.EqualTo(11));
    }

    [Test]
    public async Task DeleteBlockedSlotAsync_WhenSlotIsFromAnotherRestaurant_ReturnsNotFound()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 10);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _orderConfigRepository.GetBlockedSlotByIdAsync(9, Arg.Any<CancellationToken>())
            .Returns(new OrderBlockedSlot { Id = 9, RestaurantId = 2 });

        var result = await _sut.DeleteBlockedSlotAsync(1, 9, 10);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.BlockedSlotNotFound));
    }

    [Test]
    public async Task DeleteBlockedSlotAsync_WhenValid_DeletesSlot()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 10);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _orderConfigRepository.GetBlockedSlotByIdAsync(9, Arg.Any<CancellationToken>())
            .Returns(new OrderBlockedSlot { Id = 9, RestaurantId = 1, Restaurant = restaurant });
        _orderConfigRepository.DeleteBlockedSlotAsync(9, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteBlockedSlotAsync(1, 9, 10);

        Assert.That(result.IsSuccess, Is.True);
    }

    private static Restaurant CreateRestaurant(int id, int ownerId)
    {
        return new Restaurant
        {
            Id = id,
            OwnerId = ownerId,
            Name = "Restaurant test",
            AdressLine1 = "1 Rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "FR"
        };
    }
}
