using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Dish;
using DeliverTableSharedLibrary.Dtos.Promotion;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;
using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class PromotionServiceTests
{
    private IPromotionRepository _promotionRepository = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private IDishRepository _dishRepository = null!;
    private PromotionService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _promotionRepository = Substitute.For<IPromotionRepository>();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _dishRepository = Substitute.For<IDishRepository>();
        _sut = new PromotionService(_promotionRepository, _restaurantRepository, _dishRepository);
    }

    [Test]
    public async Task CreateAsync_WithValidAutomaticPromotion_ReturnsSuccess()
    {
        Restaurant restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        CreatePromotionRequest request = new CreatePromotionRequest
        {
            Name = "Promo Été",
            Description = "Réduction estivale",
            PromotionType = nameof(PromotionType.Automatic),
            DiscountType = nameof(DiscountType.Percentage),
            DiscountValue = 10m,
            StartsAt = new DateTime(2026, 6, 1),
            EndsAt = new DateTime(2026, 8, 31)
        };

        _promotionRepository.CreateAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                Promotion p = callInfo.ArgAt<Promotion>(0);
                p.Id = 42;
                p.PromotionDishes ??= [];
                return p;
            });

        ServiceResult<PromotionDto> result = await _sut.CreateAsync(1, 1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(42));
        Assert.That(result.Value.Name, Is.EqualTo("Promo Été"));
        Assert.That(result.Value.PromotionType, Is.EqualTo(nameof(PromotionType.Automatic)));
        Assert.That(result.Value.DiscountType, Is.EqualTo(nameof(DiscountType.Percentage)));
    }

    [Test]
    public async Task CreateAsync_WhenRestaurantNotFound_ReturnsError()
    {
        _restaurantRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        CreatePromotionRequest request = new CreatePromotionRequest
        {
            Name = "Promo",
            PromotionType = nameof(PromotionType.Automatic),
            DiscountType = nameof(DiscountType.Percentage),
            DiscountValue = 10m,
            StartsAt = new DateTime(2026, 6, 1),
            EndsAt = new DateTime(2026, 8, 31)
        };

        ServiceResult<PromotionDto> result = await _sut.CreateAsync(99, 1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RestaurantNotFound));
    }

    [Test]
    public async Task CreateAsync_WhenNotOwner_ReturnsError()
    {
        Restaurant restaurant = CreateRestaurant(ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        CreatePromotionRequest request = new CreatePromotionRequest
        {
            Name = "Promo",
            PromotionType = nameof(PromotionType.Automatic),
            DiscountType = nameof(DiscountType.Percentage),
            DiscountValue = 10m,
            StartsAt = new DateTime(2026, 6, 1),
            EndsAt = new DateTime(2026, 8, 31)
        };

        ServiceResult<PromotionDto> result = await _sut.CreateAsync(1, 999, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task CreateAsync_WithInvalidDates_ReturnsError()
    {
        Restaurant restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        CreatePromotionRequest request = new CreatePromotionRequest
        {
            Name = "Promo",
            PromotionType = nameof(PromotionType.Automatic),
            DiscountType = nameof(DiscountType.Percentage),
            DiscountValue = 10m,
            StartsAt = new DateTime(2026, 8, 31),
            EndsAt = new DateTime(2026, 6, 1)
        };

        ServiceResult<PromotionDto> result = await _sut.CreateAsync(1, 1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.InvalidPromotionDates));
    }

    [Test]
    public async Task CreateAsync_WithItemBasedAndInvalidDish_ReturnsError()
    {
        Restaurant restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        // Restaurant 1 only has dish 5 — dish 10 does not belong to it
        List<Dish> restaurantDishes = new List<Dish> { new() { Id = 5, RestaurantId = 1, Name = "Plat local" } };
        _dishRepository.GetByRestaurantIdAsync(Arg.Any<DishQuery>(), 1, Arg.Any<CancellationToken>())
            .Returns((restaurantDishes, 1));

        CreatePromotionRequest request = new CreatePromotionRequest
        {
            Name = "Promo Item",
            PromotionType = nameof(PromotionType.ItemBased),
            DiscountType = nameof(DiscountType.FixedAmount),
            DiscountValue = 5m,
            StartsAt = new DateTime(2026, 6, 1),
            EndsAt = new DateTime(2026, 8, 31),
            DishIds = [10]
        };

        ServiceResult<PromotionDto> result = await _sut.CreateAsync(1, 1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.PromotionDishNotFromRestaurant));
    }

    [Test]
    public async Task CreateAsync_WithItemBasedSpecialMenuAndZeroDiscount_ReturnsSuccess()
    {
        var restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        var restaurantDishes = new List<Dish>
        {
            new() { Id = 5, RestaurantId = 1, Name = "Tarte pascale" }
        };
        _dishRepository.GetByRestaurantIdAsync(Arg.Any<DishQuery>(), 1, Arg.Any<CancellationToken>())
            .Returns((restaurantDishes, 1));

        _promotionRepository.CreateAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var p = callInfo.ArgAt<Promotion>(0);
                p.Id = 43;
                p.PromotionDishes ??= [];
                return p;
            });

        var request = new CreatePromotionRequest
        {
            Name = "Menu de Pâques",
            Description = "Sélection spéciale du week-end",
            PromotionType = nameof(PromotionType.ItemBased),
            DiscountType = nameof(DiscountType.FixedAmount),
            DiscountValue = 0m,
            StartsAt = new DateTime(2026, 6, 1),
            EndsAt = new DateTime(2026, 8, 31),
            DishIds = [5]
        };

        var result = await _sut.CreateAsync(1, 1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(43));
        Assert.That(result.Value.Name, Is.EqualTo("Menu de Pâques"));
        Assert.That(result.Value.DishIds, Is.EqualTo([5]));
        Assert.That(result.Value.DiscountValue, Is.EqualTo(0m));
    }

    [Test]
    public async Task GetByRestaurantAsync_WhenOwner_ReturnsPaginatedResult()
    {
        Restaurant restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        List<Promotion> promotions = new List<Promotion>
        {
            new()
            {
                Id = 1,
                RestaurantId = 1,
                Name = "Promo 1",
                PromotionType = PromotionType.Automatic,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10m,
                StartsAt = new DateTime(2026, 6, 1),
                EndsAt = new DateTime(2026, 8, 31),
                PromotionDishes = []
            }
        };

        _promotionRepository.GetByRestaurantAsync(1, Arg.Any<PromotionQuery>(), Arg.Any<CancellationToken>())
            .Returns((promotions, 1));

        ServiceResult<PaginatedResult<PromotionDto>> result = await _sut.GetByRestaurantAsync(1, 1, new PromotionQuery());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.TotalCount, Is.EqualTo(1));
        Assert.That(result.Value.Items, Has.Count.EqualTo(1));
        Assert.That(result.Value.Items[0].Name, Is.EqualTo("Promo 1"));
    }

    [Test]
    public async Task GetByRestaurantAsync_WhenNotOwner_ReturnsError()
    {
        Restaurant restaurant = CreateRestaurant(ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        ServiceResult<PaginatedResult<PromotionDto>> result = await _sut.GetByRestaurantAsync(1, 999, new PromotionQuery());

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task UpdateAsync_WithValidData_ReturnsUpdatedPromotion()
    {
        Promotion promotion = CreatePromotion(restaurantId: 1);
        _promotionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(promotion);

        Restaurant restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        UpdatePromotionRequest request = new UpdatePromotionRequest
        {
            Name = "Promo Mise à jour",
            Description = "Nouvelle description",
            PromotionType = nameof(PromotionType.Automatic),
            DiscountType = nameof(DiscountType.FixedAmount),
            DiscountValue = 15m,
            StartsAt = new DateTime(2026, 7, 1),
            EndsAt = new DateTime(2026, 9, 30),
            IsActive = true
        };

        _promotionRepository.UpdateAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<Promotion>(0));

        ServiceResult<PromotionDto> result = await _sut.UpdateAsync(1, 1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Name, Is.EqualTo("Promo Mise à jour"));
        Assert.That(result.Value.DiscountType, Is.EqualTo(nameof(DiscountType.FixedAmount)));
        Assert.That(result.Value.DiscountValue, Is.EqualTo(15m));
    }

    [Test]
    public async Task UpdateAsync_WhenNotFound_ReturnsError()
    {
        _promotionRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Promotion?)null);

        UpdatePromotionRequest request = new UpdatePromotionRequest
        {
            Name = "Promo",
            PromotionType = nameof(PromotionType.Automatic),
            DiscountType = nameof(DiscountType.Percentage),
            DiscountValue = 10m,
            StartsAt = new DateTime(2026, 6, 1),
            EndsAt = new DateTime(2026, 8, 31)
        };

        ServiceResult<PromotionDto> result = await _sut.UpdateAsync(99, 1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.PromotionNotFound));
    }

    [Test]
    public async Task DeleteAsync_WhenOwner_ReturnsSuccess()
    {
        Promotion promotion = CreatePromotion(restaurantId: 1);
        _promotionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(promotion);

        Restaurant restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        _promotionRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        ServiceResult result = await _sut.DeleteAsync(1, 1);

        Assert.That(result.IsSuccess, Is.True);
        await _promotionRepository.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteAsync_WhenNotOwner_ReturnsError()
    {
        Promotion promotion = CreatePromotion(restaurantId: 1);
        _promotionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(promotion);

        Restaurant restaurant = CreateRestaurant(ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        ServiceResult result = await _sut.DeleteAsync(1, 999);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    private static Promotion CreatePromotion(int restaurantId)
    {
        return new Promotion
        {
            Id = 1,
            RestaurantId = restaurantId,
            Name = "Promo Test",
            PromotionType = PromotionType.Automatic,
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10m,
            StartsAt = new DateTime(2026, 6, 1),
            EndsAt = new DateTime(2026, 8, 31),
            PromotionDishes = []
        };
    }
}
