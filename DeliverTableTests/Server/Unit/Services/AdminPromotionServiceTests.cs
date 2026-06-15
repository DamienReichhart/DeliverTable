using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;
using DeliverTableServer.Common;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class AdminPromotionServiceTests
{
    private IPromotionRepository _promotionRepository = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private AdminPromotionService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _promotionRepository = Substitute.For<IPromotionRepository>();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _sut = new AdminPromotionService(_promotionRepository, _restaurantRepository);
    }

    #region GetAllAsync

    [Test]
    public async Task GetAllAsync_ReturnsAllPromotions()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        List<Promotion> promotions = new List<Promotion>
        {
            new() { Id = 1, Name = "Promo A", RestaurantId = 1, Restaurant = restaurant },
            new() { Id = 2, Name = "Promo B", RestaurantId = 1, Restaurant = restaurant }
        };

        _promotionRepository.GetAllUnscopedAsync(Arg.Any<CancellationToken>()).Returns(promotions);

        ServiceResult<List<AdminPromotionResponse>> result = await _sut.GetAllAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    #endregion

    #region GetByIdAsync

    [Test]
    public async Task GetByIdAsync_WhenExists_ReturnsPromotion()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        Promotion promotion = CreatePromotion(id: 1, restaurantId: 1);
        promotion.Restaurant = restaurant;

        _promotionRepository.GetByIdWithRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(promotion);

        ServiceResult<AdminPromotionResponse> result = await _sut.GetByIdAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(1));
        Assert.That(result.Value.RestaurantName, Is.EqualTo(restaurant.Name));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotExists_Returns404()
    {
        _promotionRepository.GetByIdWithRestaurantAsync(99, Arg.Any<CancellationToken>()).Returns((Promotion?)null);

        ServiceResult<AdminPromotionResponse> result = await _sut.GetByIdAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.PromotionNotFound));
    }

    #endregion

    #region CreateAsync

    [Test]
    public async Task CreateAsync_WhenRestaurantExists_CreatesPromotion()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _promotionRepository.CreateAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                Promotion p = callInfo.Arg<Promotion>();
                p.Id = 10;
                p.Restaurant = restaurant;
                return p;
            });

        AdminCreatePromotionRequest request = new AdminCreatePromotionRequest
        {
            Name = "Nouvelle Promo",
            Description = "Description",
            PromotionType = PromotionType.Automatic,
            DiscountType = DiscountType.Percentage,
            DiscountValue = 15m,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30),
            RestaurantId = 1,
            IsActive = true
        };

        ServiceResult<AdminPromotionResponse> result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Name, Is.EqualTo("Nouvelle Promo"));
        Assert.That(result.Value.DiscountValue, Is.EqualTo(15m));
        Assert.That(result.Value.PromotionType, Is.EqualTo(PromotionType.Automatic));
    }

    [Test]
    public async Task CreateAsync_WhenRestaurantNotExists_Returns404()
    {
        _restaurantRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        AdminCreatePromotionRequest request = new AdminCreatePromotionRequest
        {
            Name = "Promo",
            RestaurantId = 99,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        };

        ServiceResult<AdminPromotionResponse> result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RestaurantNotFound));
    }

    [Test]
    public async Task CreateAsync_WhenInvalidDates_Returns400()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        AdminCreatePromotionRequest request = new AdminCreatePromotionRequest
        {
            Name = "Promo",
            RestaurantId = 1,
            StartsAt = DateTime.UtcNow.AddDays(30),
            EndsAt = DateTime.UtcNow
        };

        ServiceResult<AdminPromotionResponse> result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.InvalidPromotionDates));
    }

    [Test]
    public async Task CreateAsync_WhenPercentageOver100_Returns400()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        AdminCreatePromotionRequest request = new AdminCreatePromotionRequest
        {
            Name = "Promo",
            RestaurantId = 1,
            DiscountType = DiscountType.Percentage,
            DiscountValue = 150m,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        };

        ServiceResult<AdminPromotionResponse> result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.PercentageDiscountTooHigh));
    }

    #endregion

    #region UpdateAsync

    [Test]
    public async Task UpdateAsync_WhenExists_UpdatesAndReturns()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        Promotion promotion = CreatePromotion(id: 1, restaurantId: 1);
        promotion.Restaurant = restaurant;

        _promotionRepository.GetByIdWithRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(promotion);
        _promotionRepository.UpdateAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Promotion>());

        AdminUpdatePromotionRequest request = new AdminUpdatePromotionRequest
        {
            Name = "Nouveau Nom",
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 5m,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(60),
            IsActive = false
        };

        ServiceResult<AdminPromotionResponse> result = await _sut.UpdateAsync(1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Name, Is.EqualTo("Nouveau Nom"));
        Assert.That(result.Value.DiscountType, Is.EqualTo(DiscountType.FixedAmount));
        Assert.That(result.Value.DiscountValue, Is.EqualTo(5m));
        Assert.That(result.Value.IsActive, Is.False);
    }

    [Test]
    public async Task UpdateAsync_WhenNotExists_Returns404()
    {
        _promotionRepository.GetByIdWithRestaurantAsync(99, Arg.Any<CancellationToken>()).Returns((Promotion?)null);

        AdminUpdatePromotionRequest request = new AdminUpdatePromotionRequest
        {
            Name = "Name",
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        };

        ServiceResult<AdminPromotionResponse> result = await _sut.UpdateAsync(99, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.PromotionNotFound));
    }

    [Test]
    public async Task UpdateAsync_WhenInvalidDates_Returns400()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        Promotion promotion = CreatePromotion(id: 1, restaurantId: 1);
        promotion.Restaurant = restaurant;

        _promotionRepository.GetByIdWithRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(promotion);

        AdminUpdatePromotionRequest request = new AdminUpdatePromotionRequest
        {
            Name = "Promo",
            StartsAt = DateTime.UtcNow.AddDays(30),
            EndsAt = DateTime.UtcNow
        };

        ServiceResult<AdminPromotionResponse> result = await _sut.UpdateAsync(1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.InvalidPromotionDates));
    }

    [Test]
    public async Task UpdateAsync_WhenPercentageOver100_Returns400()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        Promotion promotion = CreatePromotion(id: 1, restaurantId: 1);
        promotion.Restaurant = restaurant;

        _promotionRepository.GetByIdWithRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(promotion);

        AdminUpdatePromotionRequest request = new AdminUpdatePromotionRequest
        {
            Name = "Promo",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 101m,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        };

        ServiceResult<AdminPromotionResponse> result = await _sut.UpdateAsync(1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.PercentageDiscountTooHigh));
    }

    #endregion

    #region DeleteAsync

    [Test]
    public async Task DeleteAsync_WhenExists_ReturnsSuccess()
    {
        _promotionRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        ServiceResult result = await _sut.DeleteAsync(1);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DeleteAsync_WhenNotExists_Returns404()
    {
        _promotionRepository.DeleteAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        ServiceResult result = await _sut.DeleteAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.PromotionNotFound));
    }

    #endregion
}
