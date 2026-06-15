using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Dish;
using DeliverTableSharedLibrary.Dtos.Event;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class RestaurantEventServiceTests
{
    private IEventRepository _eventRepository = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private IDishRepository _dishRepository = null!;
    private RestaurantEventService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _dishRepository = Substitute.For<IDishRepository>();
        _sut = new RestaurantEventService(_eventRepository, _restaurantRepository, _dishRepository);
    }

    [Test]
    public async Task CreateAsync_WithValidData_ReturnsSuccess()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        _dishRepository.GetByRestaurantIdAsync(Arg.Any<DishQuery>(), 1, Arg.Any<CancellationToken>())
            .Returns((new List<Dish> { new() { Id = 5, RestaurantId = 1, Name = "Bûche de Noël" } }, 1));

        _eventRepository.CreateAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var e = callInfo.ArgAt<Event>(0);
                e.Id = 42;
                return e;
            });
        _eventRepository.GetByIdAsync(42, Arg.Any<CancellationToken>())
            .Returns(callInfo => BuildEvent(42, 1, "Menu spécial Noël", dishId: 5));

        var request = new CreateRestaurantEventRequest
        {
            Name = "Menu spécial Noël",
            StartsAt = new DateTime(2026, 12, 1),
            EndsAt = new DateTime(2026, 12, 31),
            MenuItems = [new EventMenuItemRequest { DishId = 5, OverridePrice = 29.90m }]
        };

        var result = await _sut.CreateAsync(1, 1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(42));
        Assert.That(result.Value.Name, Is.EqualTo("Menu spécial Noël"));
        Assert.That(result.Value.MenuItems, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CreateAsync_WhenNotOwner_ReturnsNotFound()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        var request = new CreateRestaurantEventRequest
        {
            Name = "Noël",
            StartsAt = new DateTime(2026, 12, 1),
            EndsAt = new DateTime(2026, 12, 31)
        };

        var result = await _sut.CreateAsync(1, 999, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task CreateAsync_WithInvalidDates_ReturnsBadRequest()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        var request = new CreateRestaurantEventRequest
        {
            Name = "Noël",
            StartsAt = new DateTime(2026, 12, 31),
            EndsAt = new DateTime(2026, 12, 1)
        };

        var result = await _sut.CreateAsync(1, 1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.InvalidEventDates));
    }

    [Test]
    public async Task CreateAsync_WithDishFromAnotherRestaurant_ReturnsError()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        _dishRepository.GetByRestaurantIdAsync(Arg.Any<DishQuery>(), 1, Arg.Any<CancellationToken>())
            .Returns((new List<Dish> { new() { Id = 5, RestaurantId = 1, Name = "Plat local" } }, 1));

        var request = new CreateRestaurantEventRequest
        {
            Name = "Noël",
            StartsAt = new DateTime(2026, 12, 1),
            EndsAt = new DateTime(2026, 12, 31),
            MenuItems = [new EventMenuItemRequest { DishId = 99 }]
        };

        var result = await _sut.CreateAsync(1, 1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.EventDishNotFromRestaurant));
    }

    [Test]
    public async Task GetByRestaurantAsync_WhenOwner_ReturnsEvents()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        _eventRepository.GetByRestaurantAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<Event> { BuildEvent(1, 1, "Saint-Valentin", dishId: 5) });

        var result = await _sut.GetByRestaurantAsync(1, 1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(result.Value![0].Name, Is.EqualTo("Saint-Valentin"));
    }

    [Test]
    public async Task UpdateAsync_WithNewTitle_ReturnsUpdatedEvent()
    {
        var existing = BuildEvent(1, 1, "Ancien titre", dishId: 5);
        // First call loads the event for update; the post-save reload returns the persisted (renamed) state.
        _eventRepository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(existing, BuildEvent(1, 1, "Menu spécial Noël", dishId: 5));

        var restaurant = CreateRestaurant(id: 1, ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        _dishRepository.GetByRestaurantIdAsync(Arg.Any<DishQuery>(), 1, Arg.Any<CancellationToken>())
            .Returns((new List<Dish> { new() { Id = 5, RestaurantId = 1, Name = "Plat" } }, 1));

        _eventRepository.UpdateAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<Event>(0));

        var request = new UpdateRestaurantEventRequest
        {
            Name = "Menu spécial Noël",
            StartsAt = new DateTime(2026, 12, 1),
            EndsAt = new DateTime(2026, 12, 31),
            IsActive = true,
            MenuItems = [new EventMenuItemRequest { DishId = 5 }]
        };

        var result = await _sut.UpdateAsync(1, 1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Name, Is.EqualTo("Menu spécial Noël"));
    }

    [Test]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFound()
    {
        _eventRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Event?)null);

        var request = new UpdateRestaurantEventRequest
        {
            Name = "X",
            StartsAt = new DateTime(2026, 12, 1),
            EndsAt = new DateTime(2026, 12, 31)
        };

        var result = await _sut.UpdateAsync(99, 1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.EventNotFound));
    }

    [Test]
    public async Task DeleteAsync_WhenOwner_ReturnsSuccess()
    {
        var existing = BuildEvent(1, 1, "Noël", dishId: 5);
        _eventRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var restaurant = CreateRestaurant(id: 1, ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        var result = await _sut.DeleteAsync(1, 1);

        Assert.That(result.IsSuccess, Is.True);
        await _eventRepository.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetActiveByRestaurantAsync_ReturnsActiveEvents_WithoutOwnership()
    {
        _eventRepository.GetActiveByRestaurantAsync(1, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<Event> { BuildEvent(7, 1, "Menu spécial Noël", dishId: 5) });

        var result = await _sut.GetActiveByRestaurantAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
        Assert.That(result.Value![0].Name, Is.EqualTo("Menu spécial Noël"));
    }

    private static Event BuildEvent(int id, int restaurantId, string name, int dishId)
        => new()
        {
            Id = id,
            RestaurantId = restaurantId,
            CreatedByUserId = 1,
            Name = name,
            StartsAt = new DateTime(2026, 12, 1),
            EndsAt = new DateTime(2026, 12, 31),
            IsActive = true,
            EventMenuItems =
            [
                new EventMenuItem { Id = 1, DishId = dishId, Dish = new Dish { Id = dishId, Name = "Plat", RestaurantId = restaurantId } }
            ]
        };
}
