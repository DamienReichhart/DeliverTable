using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Admin;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class AdminOrderConfigServiceTests
{
    private IOrderConfigRepository _orderConfigRepository = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private AdminOrderConfigService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _orderConfigRepository = Substitute.For<IOrderConfigRepository>();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _sut = new AdminOrderConfigService(_orderConfigRepository, _restaurantRepository);
    }

    #region GetAllRulesAsync

    [Test]
    public async Task GetAllRulesAsync_ReturnsAllRules()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var rules = new List<OrderRule>
        {
            new() { Id = 1, RestaurantId = 1, Restaurant = restaurant, AllowPreorder = true },
            new() { Id = 2, RestaurantId = 1, Restaurant = restaurant, AllowDelivery = true }
        };

        _orderConfigRepository.GetAllRulesAsync(Arg.Any<CancellationToken>()).Returns(rules);

        var result = await _sut.GetAllRulesAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    #endregion

    #region GetRuleByIdAsync

    [Test]
    public async Task GetRuleByIdAsync_WhenExists_ReturnsRule()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var rule = new OrderRule
        {
            Id = 1,
            RestaurantId = 1,
            Restaurant = restaurant,
            MinConfirmAmount = 15.50m,
            AllowPreorder = true
        };

        _orderConfigRepository.GetRuleByIdAsync(1, Arg.Any<CancellationToken>()).Returns(rule);

        var result = await _sut.GetRuleByIdAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(1));
        Assert.That(result.Value.MinConfirmAmount, Is.EqualTo(15.50m));
        Assert.That(result.Value.RestaurantName, Is.EqualTo(restaurant.Name));
    }

    [Test]
    public async Task GetRuleByIdAsync_WhenNotExists_Returns404()
    {
        _orderConfigRepository.GetRuleByIdAsync(99, Arg.Any<CancellationToken>()).Returns((OrderRule?)null);

        var result = await _sut.GetRuleByIdAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.OrderRuleNotFound));
    }

    #endregion

    #region CreateRuleAsync

    [Test]
    public async Task CreateRuleAsync_WhenValid_CreatesRule()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _orderConfigRepository.CreateRuleAsync(Arg.Any<OrderRule>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var r = callInfo.Arg<OrderRule>();
                r.Id = 10;
                r.Restaurant = restaurant;
                return r;
            });

        var request = new AdminCreateOrderRuleRequest
        {
            RestaurantId = 1,
            MinConfirmAmount = 20m,
            MinLeadTimeHours = 2,
            MaxAdvanceDays = 7,
            SlotDurationMinutes = 30,
            AvailabilityRanges = "09:00-12:00",
            AllowPreorder = true,
            AllowDelivery = false
        };

        var result = await _sut.CreateRuleAsync(request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.MinConfirmAmount, Is.EqualTo(20m));
        Assert.That(result.Value.AllowPreorder, Is.True);
        Assert.That(result.Value.AllowDelivery, Is.False);
    }

    [Test]
    public async Task CreateRuleAsync_WhenRestaurantNotFound_Returns404()
    {
        _restaurantRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        var request = new AdminCreateOrderRuleRequest
        {
            RestaurantId = 99,
            AllowPreorder = true
        };

        var result = await _sut.CreateRuleAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RestaurantNotFound));
    }

    #endregion

    #region UpdateRuleAsync

    [Test]
    public async Task UpdateRuleAsync_WhenExists_UpdatesAndReturns()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var rule = new OrderRule
        {
            Id = 1,
            RestaurantId = 1,
            Restaurant = restaurant,
            MinConfirmAmount = 10m,
            AllowPreorder = false
        };

        _orderConfigRepository.GetRuleByIdAsync(1, Arg.Any<CancellationToken>()).Returns(rule);
        _orderConfigRepository.UpdateRuleAsync(Arg.Any<OrderRule>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<OrderRule>());

        var request = new AdminUpdateOrderRuleRequest
        {
            MinConfirmAmount = 25m,
            MinLeadTimeHours = 3,
            MaxAdvanceDays = 14,
            SlotDurationMinutes = 60,
            AvailabilityRanges = "10:00-18:00",
            AllowPreorder = true,
            AllowDelivery = true
        };

        var result = await _sut.UpdateRuleAsync(1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.MinConfirmAmount, Is.EqualTo(25m));
        Assert.That(result.Value.AllowPreorder, Is.True);
        Assert.That(result.Value.AllowDelivery, Is.True);
        Assert.That(result.Value.SlotDurationMinutes, Is.EqualTo(60));
    }

    [Test]
    public async Task UpdateRuleAsync_WhenNotExists_Returns404()
    {
        _orderConfigRepository.GetRuleByIdAsync(99, Arg.Any<CancellationToken>()).Returns((OrderRule?)null);

        var request = new AdminUpdateOrderRuleRequest
        {
            AllowPreorder = true
        };

        var result = await _sut.UpdateRuleAsync(99, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.OrderRuleNotFound));
    }

    #endregion

    #region DeleteRuleAsync

    [Test]
    public async Task DeleteRuleAsync_WhenExists_ReturnsSuccess()
    {
        _orderConfigRepository.DeleteRuleAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteRuleAsync(1);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DeleteRuleAsync_WhenNotExists_Returns404()
    {
        _orderConfigRepository.DeleteRuleAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.DeleteRuleAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.OrderRuleNotFound));
    }

    #endregion

    #region GetAllBlockedSlotsAsync

    [Test]
    public async Task GetAllBlockedSlotsAsync_ReturnsAllSlots()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var slots = new List<OrderBlockedSlot>
        {
            new()
            {
                Id = 1, RestaurantId = 1, Restaurant = restaurant,
                StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddHours(2)
            },
            new()
            {
                Id = 2, RestaurantId = 1, Restaurant = restaurant,
                StartsAt = DateTime.UtcNow.AddDays(1), EndsAt = DateTime.UtcNow.AddDays(1).AddHours(2)
            }
        };

        _orderConfigRepository.GetAllBlockedSlotsAsync(Arg.Any<CancellationToken>()).Returns(slots);

        var result = await _sut.GetAllBlockedSlotsAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    #endregion

    #region GetBlockedSlotByIdAsync

    [Test]
    public async Task GetBlockedSlotByIdAsync_WhenExists_ReturnsSlot()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var slot = new OrderBlockedSlot
        {
            Id = 1,
            RestaurantId = 1,
            Restaurant = restaurant,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(2),
            Reason = "Maintenance"
        };

        _orderConfigRepository.GetBlockedSlotByIdAsync(1, Arg.Any<CancellationToken>()).Returns(slot);

        var result = await _sut.GetBlockedSlotByIdAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(1));
        Assert.That(result.Value.Reason, Is.EqualTo("Maintenance"));
        Assert.That(result.Value.RestaurantName, Is.EqualTo(restaurant.Name));
    }

    [Test]
    public async Task GetBlockedSlotByIdAsync_WhenNotExists_Returns404()
    {
        _orderConfigRepository.GetBlockedSlotByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns((OrderBlockedSlot?)null);

        var result = await _sut.GetBlockedSlotByIdAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.BlockedSlotNotFound));
    }

    #endregion

    #region CreateBlockedSlotAsync

    [Test]
    public async Task CreateBlockedSlotAsync_WhenValid_CreatesSlot()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _orderConfigRepository.CreateBlockedSlotAsync(Arg.Any<OrderBlockedSlot>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var s = callInfo.Arg<OrderBlockedSlot>();
                s.Id = 10;
                s.Restaurant = restaurant;
                return s;
            });

        var request = new AdminCreateBlockedSlotRequest
        {
            RestaurantId = 1,
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            Reason = "Fermeture exceptionnelle"
        };

        var result = await _sut.CreateBlockedSlotAsync(request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Reason, Is.EqualTo("Fermeture exceptionnelle"));
    }

    [Test]
    public async Task CreateBlockedSlotAsync_WhenInvalidDates_ReturnsError()
    {
        var request = new AdminCreateBlockedSlotRequest
        {
            RestaurantId = 1,
            StartsAt = DateTime.UtcNow.AddDays(2),
            EndsAt = DateTime.UtcNow.AddDays(1),
            Reason = "Test"
        };

        var result = await _sut.CreateBlockedSlotAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.InvalidBlockedSlotDates));
    }

    [Test]
    public async Task CreateBlockedSlotAsync_WhenRestaurantNotFound_Returns404()
    {
        _restaurantRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        var request = new AdminCreateBlockedSlotRequest
        {
            RestaurantId = 99,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(1)
        };

        var result = await _sut.CreateBlockedSlotAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RestaurantNotFound));
    }

    #endregion

    #region DeleteBlockedSlotAsync

    [Test]
    public async Task DeleteBlockedSlotAsync_WhenExists_ReturnsSuccess()
    {
        _orderConfigRepository.DeleteBlockedSlotAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteBlockedSlotAsync(1);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DeleteBlockedSlotAsync_WhenNotExists_Returns404()
    {
        _orderConfigRepository.DeleteBlockedSlotAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.DeleteBlockedSlotAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.BlockedSlotNotFound));
    }

    #endregion
}
