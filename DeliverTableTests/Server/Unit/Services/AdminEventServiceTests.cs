using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class AdminEventServiceTests
{
    private IEventRepository _eventRepository = null!;
    private IUserRepository _userRepository = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private AdminEventService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _sut = new AdminEventService(_eventRepository, _userRepository, _restaurantRepository);
    }

    #region GetAllAsync

    [Test]
    public async Task GetAllAsync_ReturnsAllEvents()
    {
        var user = CreateValidUser();
        user.Id = 5;
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var events = new List<Event>
        {
            new()
            {
                Id = 1, Name = "Événement A", RestaurantId = 1, Restaurant = restaurant,
                CreatedByUserId = 5, CreatedByUser = user,
                StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddHours(2)
            },
            new()
            {
                Id = 2, Name = "Événement B",
                CreatedByUserId = 5, CreatedByUser = user,
                StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddHours(3)
            }
        };

        _eventRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(events);

        var result = await _sut.GetAllAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    #endregion

    #region GetByIdAsync

    [Test]
    public async Task GetByIdAsync_WhenExists_ReturnsEvent()
    {
        var user = CreateValidUser();
        user.Id = 5;
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var evt = new Event
        {
            Id = 1,
            Name = "Événement A",
            RestaurantId = 1,
            Restaurant = restaurant,
            CreatedByUserId = 5,
            CreatedByUser = user,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(2)
        };

        _eventRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(evt);

        var result = await _sut.GetByIdAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(1));
        Assert.That(result.Value.RestaurantName, Is.EqualTo(restaurant.Name));
        Assert.That(result.Value.CreatedByUserName, Is.EqualTo($"{user.FirstName} {user.LastName}"));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotExists_Returns404()
    {
        _eventRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Event?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.EventNotFound));
    }

    #endregion

    #region CreateAsync

    [Test]
    public async Task CreateAsync_WhenValid_CreatesEvent()
    {
        var user = CreateValidUser();
        user.Id = 5;
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);

        _userRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(user);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _eventRepository.CreateAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var e = callInfo.Arg<Event>();
                e.Id = 10;
                e.CreatedByUser = user;
                e.Restaurant = restaurant;
                return e;
            });

        var request = new AdminCreateEventRequest
        {
            Name = "Nouvel Événement",
            Description = "Description",
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            MaxGuests = 50,
            Visibility = EventVisibility.Public,
            IsActive = true,
            RestaurantId = 1,
            CreatedByUserId = 5
        };

        var result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Name, Is.EqualTo("Nouvel Événement"));
        Assert.That(result.Value.MaxGuests, Is.EqualTo(50));
    }

    [Test]
    public async Task CreateAsync_WhenInvalidDates_ReturnsError()
    {
        var user = CreateValidUser();
        user.Id = 5;
        _userRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(user);

        var request = new AdminCreateEventRequest
        {
            Name = "Événement",
            StartsAt = DateTime.UtcNow.AddDays(2),
            EndsAt = DateTime.UtcNow.AddDays(1),
            CreatedByUserId = 5
        };

        var result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.InvalidEventDates));
    }

    [Test]
    public async Task CreateAsync_WhenUserNotFound_Returns404()
    {
        _userRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);

        var request = new AdminCreateEventRequest
        {
            Name = "Événement",
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(1),
            CreatedByUserId = 99
        };

        var result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task CreateAsync_WhenRestaurantNotFound_Returns404()
    {
        var user = CreateValidUser();
        user.Id = 5;
        _userRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(user);
        _restaurantRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        var request = new AdminCreateEventRequest
        {
            Name = "Événement",
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(1),
            CreatedByUserId = 5,
            RestaurantId = 99
        };

        var result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RestaurantNotFound));
    }

    [Test]
    public async Task CreateAsync_WhenNoRestaurant_CreatesEvent()
    {
        var user = CreateValidUser();
        user.Id = 5;
        _userRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(user);
        _eventRepository.CreateAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var e = callInfo.Arg<Event>();
                e.Id = 10;
                e.CreatedByUser = user;
                return e;
            });

        var request = new AdminCreateEventRequest
        {
            Name = "Événement sans restaurant",
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(1),
            CreatedByUserId = 5,
            RestaurantId = null
        };

        var result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.RestaurantId, Is.Null);
    }

    #endregion

    #region UpdateAsync

    [Test]
    public async Task UpdateAsync_WhenExists_UpdatesAndReturns()
    {
        var user = CreateValidUser();
        user.Id = 5;
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var evt = new Event
        {
            Id = 1,
            Name = "Ancien",
            RestaurantId = 1,
            Restaurant = restaurant,
            CreatedByUserId = 5,
            CreatedByUser = user,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(1)
        };

        _eventRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(evt);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _eventRepository.UpdateAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Event>());

        var request = new AdminUpdateEventRequest
        {
            Name = "Nouveau Nom",
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(3),
            MaxGuests = 100,
            Visibility = EventVisibility.Private,
            IsActive = false,
            RestaurantId = 1
        };

        var result = await _sut.UpdateAsync(1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Name, Is.EqualTo("Nouveau Nom"));
        Assert.That(result.Value.MaxGuests, Is.EqualTo(100));
        Assert.That(result.Value.Visibility, Is.EqualTo(EventVisibility.Private));
        Assert.That(result.Value.IsActive, Is.False);
    }

    [Test]
    public async Task UpdateAsync_WhenNotExists_Returns404()
    {
        _eventRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Event?)null);

        var request = new AdminUpdateEventRequest
        {
            Name = "Name",
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(1)
        };

        var result = await _sut.UpdateAsync(99, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.EventNotFound));
    }

    [Test]
    public async Task UpdateAsync_WhenInvalidDates_ReturnsError()
    {
        var user = CreateValidUser();
        user.Id = 5;
        var evt = new Event
        {
            Id = 1,
            Name = "Événement",
            CreatedByUserId = 5,
            CreatedByUser = user,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(1)
        };

        _eventRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(evt);

        var request = new AdminUpdateEventRequest
        {
            Name = "Name",
            StartsAt = DateTime.UtcNow.AddDays(2),
            EndsAt = DateTime.UtcNow.AddDays(1)
        };

        var result = await _sut.UpdateAsync(1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.InvalidEventDates));
    }

    #endregion

    #region DeleteAsync

    [Test]
    public async Task DeleteAsync_WhenExists_ReturnsSuccess()
    {
        _eventRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteAsync(1);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DeleteAsync_WhenNotExists_Returns404()
    {
        _eventRepository.DeleteAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.DeleteAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.EventNotFound));
    }

    #endregion
}
