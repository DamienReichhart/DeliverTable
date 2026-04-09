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
public class AdminDiscountCodeServiceTests
{
    private IDiscountCodeRepository _discountCodeRepository = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private AdminDiscountCodeService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _discountCodeRepository = Substitute.For<IDiscountCodeRepository>();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _sut = new AdminDiscountCodeService(_discountCodeRepository, _restaurantRepository);
    }

    #region GetAllAsync

    [Test]
    public async Task GetAllAsync_ReturnsAllDiscountCodes()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var codes = new List<DiscountCode>
        {
            new() { Id = 1, Code = "CODE1", RestaurantId = 1, Restaurant = restaurant, Redemptions = [] },
            new() { Id = 2, Code = "CODE2", RestaurantId = 1, Restaurant = restaurant, Redemptions = [] }
        };

        _discountCodeRepository.GetAllUnscopedAsync(Arg.Any<CancellationToken>()).Returns(codes);

        var result = await _sut.GetAllAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    #endregion

    #region GetByIdAsync

    [Test]
    public async Task GetByIdAsync_WhenExists_ReturnsDiscountCode()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var code = CreateDiscountCode(id: 1, restaurantId: 1);
        code.Restaurant = restaurant;
        code.Redemptions = [];

        _discountCodeRepository.GetByIdWithRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(code);

        var result = await _sut.GetByIdAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(1));
        Assert.That(result.Value.RestaurantName, Is.EqualTo(restaurant.Name));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotExists_Returns404()
    {
        _discountCodeRepository.GetByIdWithRestaurantAsync(99, Arg.Any<CancellationToken>()).Returns((DiscountCode?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.DiscountCodeNotFound));
    }

    #endregion

    #region CreateAsync

    [Test]
    public async Task CreateAsync_WhenRestaurantExists_CreatesDiscountCode()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _discountCodeRepository.GetByCodeAndRestaurantAsync("NEWCODE", 1, Arg.Any<CancellationToken>())
            .Returns((DiscountCode?)null);
        _discountCodeRepository.CreateAsync(Arg.Any<DiscountCode>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var dc = callInfo.Arg<DiscountCode>();
                dc.Id = 10;
                dc.Restaurant = restaurant;
                dc.Redemptions = [];
                return dc;
            });

        var request = new AdminCreateDiscountCodeRequest
        {
            Code = "NEWCODE",
            Description = "Test description",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 15m,
            ValidFrom = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            RestaurantId = 1,
            IsActive = true
        };

        var result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Code, Is.EqualTo("NEWCODE"));
        Assert.That(result.Value.DiscountValue, Is.EqualTo(15m));
    }

    [Test]
    public async Task CreateAsync_WhenRestaurantNotExists_Returns404()
    {
        _restaurantRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        var request = new AdminCreateDiscountCodeRequest
        {
            Code = "CODE",
            RestaurantId = 99,
            ValidFrom = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30)
        };

        var result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RestaurantNotFound));
    }

    [Test]
    public async Task CreateAsync_WhenInvalidDates_Returns400()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        var request = new AdminCreateDiscountCodeRequest
        {
            Code = "CODE",
            RestaurantId = 1,
            ValidFrom = DateTime.UtcNow.AddDays(30),
            ValidUntil = DateTime.UtcNow
        };

        var result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.InvalidDiscountCodeDates));
    }

    [Test]
    public async Task CreateAsync_WhenPercentageOver100_Returns400()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        var request = new AdminCreateDiscountCodeRequest
        {
            Code = "CODE",
            RestaurantId = 1,
            DiscountType = DiscountType.Percentage,
            DiscountValue = 150m,
            ValidFrom = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30)
        };

        var result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.PercentageDiscountTooHigh));
    }

    [Test]
    public async Task CreateAsync_WhenCodeAlreadyExists_Returns400()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _discountCodeRepository.GetByCodeAndRestaurantAsync("EXISTING", 1, Arg.Any<CancellationToken>())
            .Returns(CreateDiscountCode(id: 5, restaurantId: 1, code: "EXISTING"));

        var request = new AdminCreateDiscountCodeRequest
        {
            Code = "EXISTING",
            RestaurantId = 1,
            ValidFrom = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30)
        };

        var result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.DiscountCodeAlreadyExists));
    }

    #endregion

    #region UpdateAsync

    [Test]
    public async Task UpdateAsync_WhenExists_UpdatesAndReturns()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var code = CreateDiscountCode(id: 1, restaurantId: 1);
        code.Restaurant = restaurant;
        code.Redemptions = [];

        _discountCodeRepository.GetByIdWithRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(code);
        _discountCodeRepository.UpdateAsync(Arg.Any<DiscountCode>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<DiscountCode>());

        var request = new AdminUpdateDiscountCodeRequest
        {
            Description = "Mise à jour",
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 5m,
            ValidFrom = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(60),
            IsActive = false
        };

        var result = await _sut.UpdateAsync(1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Description, Is.EqualTo("Mise à jour"));
        Assert.That(result.Value.DiscountType, Is.EqualTo(DiscountType.FixedAmount));
        Assert.That(result.Value.DiscountValue, Is.EqualTo(5m));
        Assert.That(result.Value.IsActive, Is.False);
    }

    [Test]
    public async Task UpdateAsync_WhenNotExists_Returns404()
    {
        _discountCodeRepository.GetByIdWithRestaurantAsync(99, Arg.Any<CancellationToken>()).Returns((DiscountCode?)null);

        var request = new AdminUpdateDiscountCodeRequest
        {
            ValidFrom = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30)
        };

        var result = await _sut.UpdateAsync(99, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.DiscountCodeNotFound));
    }

    [Test]
    public async Task UpdateAsync_WhenInvalidDates_Returns400()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var code = CreateDiscountCode(id: 1, restaurantId: 1);
        code.Restaurant = restaurant;
        code.Redemptions = [];

        _discountCodeRepository.GetByIdWithRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(code);

        var request = new AdminUpdateDiscountCodeRequest
        {
            ValidFrom = DateTime.UtcNow.AddDays(30),
            ValidUntil = DateTime.UtcNow
        };

        var result = await _sut.UpdateAsync(1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.InvalidDiscountCodeDates));
    }

    [Test]
    public async Task UpdateAsync_WhenPercentageOver100_Returns400()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var code = CreateDiscountCode(id: 1, restaurantId: 1);
        code.Restaurant = restaurant;
        code.Redemptions = [];

        _discountCodeRepository.GetByIdWithRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(code);

        var request = new AdminUpdateDiscountCodeRequest
        {
            DiscountType = DiscountType.Percentage,
            DiscountValue = 101m,
            ValidFrom = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30)
        };

        var result = await _sut.UpdateAsync(1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.PercentageDiscountTooHigh));
    }

    #endregion

    #region DeleteAsync

    [Test]
    public async Task DeleteAsync_WhenExists_ReturnsSuccess()
    {
        _discountCodeRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteAsync(1);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DeleteAsync_WhenNotExists_Returns404()
    {
        _discountCodeRepository.DeleteAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.DeleteAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.DiscountCodeNotFound));
    }

    #endregion

    #region GetRedemptionsAsync

    [Test]
    public async Task GetRedemptionsAsync_WhenCodeExists_ReturnsRedemptions()
    {
        var code = CreateDiscountCode(id: 1, restaurantId: 1);
        _discountCodeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(code);

        var customer = CreateValidUser();
        customer.Id = 10;
        var redemptions = new List<DiscountCodeRedemption>
        {
            new() { Id = 1, DiscountCodeId = 1, CustomerId = 10, Customer = customer, OrderId = 100, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, DiscountCodeId = 1, CustomerId = 10, Customer = customer, OrderId = 101, CreatedAt = DateTime.UtcNow }
        };

        _discountCodeRepository.GetRedemptionsByCodeIdAsync(1, Arg.Any<CancellationToken>()).Returns(redemptions);

        var result = await _sut.GetRedemptionsAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
        Assert.That(result.Value![0].OrderId, Is.EqualTo(100));
    }

    [Test]
    public async Task GetRedemptionsAsync_WhenCodeNotExists_Returns404()
    {
        _discountCodeRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((DiscountCode?)null);

        var result = await _sut.GetRedemptionsAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.DiscountCodeNotFound));
    }

    #endregion
}
