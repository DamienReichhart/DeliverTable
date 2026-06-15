using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using NSubstitute;
using System.Text.Json;
using DeliverTableServer.Common;

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
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 10);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _orderConfigRepository.GetRuleByRestaurantIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new OrderRule
            {
                RestaurantId = 1,
                TablesCapacityPerSlot = 3
            });
        _restaurantRepository.CountActiveTablesByMaxCapacityAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(4);

        ServiceResult<TablesCapacityResponse> result = await _sut.GetTablesCapacityAsync(1, 999);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.RestaurantId, Is.EqualTo(1));
        Assert.That(result.Value.CapacityPerSlot, Is.EqualTo(3));
        Assert.That(result.Value.ActiveTablesFallback, Is.EqualTo(4));
    }

    [Test]
    public async Task GetBlockedSlotsAsync_WhenOwnerMatches_ReturnsSlots()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 10);
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

        ServiceResult<List<AdminBlockedSlotResponse>> result = await _sut.GetBlockedSlotsAsync(1, 10);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CreateBlockedSlotAsync_WhenDatesInvalid_ReturnsError()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 10);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        AdminCreateBlockedSlotRequest request = new AdminCreateBlockedSlotRequest
        {
            StartsAt = DateTime.UtcNow.AddHours(5),
            EndsAt = DateTime.UtcNow.AddHours(4)
        };

        ServiceResult<AdminBlockedSlotResponse> result = await _sut.CreateBlockedSlotAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.InvalidBlockedSlotDates));
    }

    [Test]
    public async Task CreateBlockedSlotAsync_WhenOverlapExists_ReturnsError()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 10);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _orderConfigRepository.ExistsBlockedSlotOverlapAsync(
                1,
                Arg.Any<int?>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        AdminCreateBlockedSlotRequest request = new AdminCreateBlockedSlotRequest
        {
            StartsAt = DateTime.UtcNow.AddHours(4),
            EndsAt = DateTime.UtcNow.AddHours(6),
            Reason = "Maintenance"
        };

        ServiceResult<AdminBlockedSlotResponse> result = await _sut.CreateBlockedSlotAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.BlockedSlotOverlapExists));
    }

    [Test]
    public async Task CreateBlockedSlotAsync_WhenValid_CreatesSlot()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 10);
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
                OrderBlockedSlot slot = callInfo.Arg<OrderBlockedSlot>();
                slot.Id = 11;
                slot.Restaurant = restaurant;
                return slot;
            });

        AdminCreateBlockedSlotRequest request = new AdminCreateBlockedSlotRequest
        {
            StartsAt = DateTime.UtcNow.AddHours(4),
            EndsAt = DateTime.UtcNow.AddHours(6),
            Reason = "Maintenance"
        };

        ServiceResult<AdminBlockedSlotResponse> result = await _sut.CreateBlockedSlotAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value!.Id, Is.EqualTo(11));
    }

    [Test]
    public async Task DeleteBlockedSlotAsync_WhenSlotIsFromAnotherRestaurant_ReturnsNotFound()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 10);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _orderConfigRepository.GetBlockedSlotByIdAsync(9, Arg.Any<CancellationToken>())
            .Returns(new OrderBlockedSlot { Id = 9, RestaurantId = 2 });

        ServiceResult result = await _sut.DeleteBlockedSlotAsync(1, 9, 10);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.BlockedSlotNotFound));
    }

    [Test]
    public async Task DeleteBlockedSlotAsync_WhenValid_DeletesSlot()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 10);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _orderConfigRepository.GetBlockedSlotByIdAsync(9, Arg.Any<CancellationToken>())
            .Returns(new OrderBlockedSlot { Id = 9, RestaurantId = 1, Restaurant = restaurant });
        _orderConfigRepository.DeleteBlockedSlotAsync(9, Arg.Any<CancellationToken>()).Returns(true);

        ServiceResult result = await _sut.DeleteBlockedSlotAsync(1, 9, 10);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task GetAvailableSlotsAsync_UsesMondayZeroDayConvention()
    {
        // Le client encode 0 = Lundi .. 6 = Dimanche. Le service doit convertir
        // DateTime.DayOfWeek (0 = Dimanche) vers cette convention, sinon les créneaux
        // sont calculés sur le mauvais jour (bug du décalage).
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 10);
        restaurant.IsActive = true;
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        DateTime date = new DateTime(2026, 6, 8, 0, 0, 0, DateTimeKind.Unspecified);
        int clientDay = ((int)date.DayOfWeek + 6) % 7;

        List<OpeningDayScheduleDto> days = new List<OpeningDayScheduleDto>();
        for (int d = 0; d <= 6; d++)
        {
            days.Add(new OpeningDayScheduleDto
            {
                DayOfWeek = d,
                Slots = d == clientDay
                    ? [new OpeningHourSlotDto { StartTime = "12:00", EndTime = "13:00" }]
                    : []
            });
        }

        _orderConfigRepository.GetRuleByRestaurantIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new OrderRule
            {
                RestaurantId = 1,
                SlotDurationMinutes = 60,
                TablesCapacityPerSlot = 5,
                AvailabilityRanges = JsonSerializer.Serialize(days)
            });
        _orderConfigRepository.IsRestaurantLevelSlotBlockedAsync(
            1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(false);
        _orderRepository.GetScheduledDineInReservedTableUnitsOverlappingAsync(
            1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0);

        ServiceResult<RestaurantAvailableSlotsResponse> result = await _sut.GetAvailableSlotsAsync(
            1, new RestaurantAvailableSlotsQuery { Date = date, GuestCount = 2 });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Slots, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task UpdateOpeningHoursAsync_WhenSlotDurationBelowMinimum_ReturnsError()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 10);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        UpdateRestaurantOpeningHoursRequest request = new UpdateRestaurantOpeningHoursRequest
        {
            SlotDurationMinutes = 1,
            Days = []
        };

        ServiceResult<RestaurantOpeningHoursResponse> result = await _sut.UpdateOpeningHoursAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.InvalidSlotDuration));
    }

    [Test]
    public async Task UpdateOpeningHoursAsync_WhenSlotDurationAboveMaximum_ReturnsError()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 10);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        UpdateRestaurantOpeningHoursRequest request = new UpdateRestaurantOpeningHoursRequest
        {
            SlotDurationMinutes = 300,
            Days = []
        };

        ServiceResult<RestaurantOpeningHoursResponse> result = await _sut.UpdateOpeningHoursAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.InvalidSlotDuration));
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
