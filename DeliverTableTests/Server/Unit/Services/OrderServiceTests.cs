using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Helpers;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class OrderServiceTests
{
    private IOrderRepository _orderRepository = null!;
    private ICartRepository _cartRepository = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private IRestaurantTransactionRepository _transactionRepository = null!;
    private IPromotionRepository _promotionRepository = null!;
    private IDiscountCodeRepository _discountCodeRepository = null!;
    private ILoyaltyRepository _loyaltyRepository = null!;
    private AppEnvironment _appEnvironment = null!;
    private OrderService _sut = null!;

    private const int CustomerId = 10;
    private const int RestaurantId = 1;

    [SetUp]
    public void SetUp()
    {
        _orderRepository = Substitute.For<IOrderRepository>();
        _cartRepository = Substitute.For<ICartRepository>();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _transactionRepository = Substitute.For<IRestaurantTransactionRepository>();
        _promotionRepository = Substitute.For<IPromotionRepository>();
        _discountCodeRepository = Substitute.For<IDiscountCodeRepository>();
        _loyaltyRepository = Substitute.For<ILoyaltyRepository>();

        _appEnvironment = AppEnvironmentTestHelper.SetupEnvironment();

        _sut = new OrderService(
            _orderRepository, _cartRepository, _restaurantRepository,
            _transactionRepository, _promotionRepository, _discountCodeRepository,
            _loyaltyRepository, _appEnvironment);
    }

    [TearDown]
    public void TearDown() => AppEnvironmentTestHelper.CleanupEnvironment();

    private static Cart CreateCartWithItems(decimal price1 = 20m, decimal price2 = 30m) => new()
    {
        Id = 1,
        CustomerId = CustomerId,
        RestaurantId = RestaurantId,
        Items =
        [
            new CartItem { DishId = 100, Dish = new Dish { Id = 100, Name = "Plat A" }, Quantity = 2, UnitPrice = price1 },
            new CartItem { DishId = 200, Dish = new Dish { Id = 200, Name = "Plat B" }, Quantity = 1, UnitPrice = price2 }
        ]
    };
    // Total = 2*20 + 1*30 = 70

    private static CreateOrderRequest CreateBaseRequest(string? discountCode = null, int loyaltyPoints = 0) => new()
    {
        RestaurantId = RestaurantId,
        OrderType = nameof(OrderType.Delivery),
        DeliveryAddress = "123 Rue Test",
        GuestCount = 1,
        DiscountCodes = discountCode is not null ? [discountCode] : [],
        LoyaltyPointsToRedeem = loyaltyPoints
    };

    private void SetupBaseCreateMocks(Restaurant? restaurant = null, Cart? cart = null)
    {
        restaurant ??= CreateRestaurant();
        cart ??= CreateCartWithItems();

        _restaurantRepository.GetByIdAsync(RestaurantId, Arg.Any<CancellationToken>())
            .Returns(restaurant);
        _cartRepository.GetByCustomerAndRestaurantAsync(CustomerId, RestaurantId, Arg.Any<CancellationToken>())
            .Returns(cart);
        _orderRepository.CreateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Order>());
        _orderRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                // Return an order with Restaurant set for the mapper
                var order = new Order
                {
                    Restaurant = restaurant,
                    Items = [],
                    Discounts = []
                };
                return order;
            });
        _cartRepository.DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _promotionRepository.GetActiveByRestaurantAsync(RestaurantId, Arg.Any<CancellationToken>())
            .Returns(new List<Promotion>());
    }

    // ─── Existing UpdateStatus tests ────────────────────────────────────────

    [Test]
    public async Task UpdateStatusAsync_WhenDelivered_CreditsRestaurantAccount()
    {
        var restaurant = new Restaurant
        {
            Id = 1,
            Name = "Test",
            Balance = 0m,
            AdressLine1 = "1 Rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "FR"
        };
        var order = new Order
        {
            Id = 10,
            RestaurantId = 1,
            Restaurant = restaurant,
            TotalAmount = 400m,
            Status = OrderStatus.Ready,
            Items = []
        };
        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = nameof(OrderStatus.Delivered) });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(restaurant.Balance, Is.EqualTo(360m));
        await _transactionRepository.Received(1).CreateAsync(
            Arg.Is<RestaurantTransaction>(t =>
                t.Type == TransactionType.Credit &&
                t.GrossAmount == 400m &&
                t.CommissionAmount == 40m &&
                t.NetAmount == 360m &&
                t.BalanceAfter == 360m),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStatusAsync_WhenNotDelivered_DoesNotCreditRestaurant()
    {
        var restaurant = new Restaurant
        {
            Id = 1,
            Name = "Test",
            Balance = 0m,
            AdressLine1 = "1 Rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "FR"
        };
        var order = new Order
        {
            Id = 10,
            RestaurantId = 1,
            Restaurant = restaurant,
            TotalAmount = 400m,
            Status = OrderStatus.Confirmed,
            Items = []
        };
        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = nameof(OrderStatus.Preparing) });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(restaurant.Balance, Is.EqualTo(0m));
        await _transactionRepository.DidNotReceive().CreateAsync(Arg.Any<RestaurantTransaction>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStatusAsync_WhenDelivered_WithActiveLoyaltyProgram_EarnsPoints()
    {
        var restaurant = new Restaurant
        {
            Id = 1,
            Name = "Test",
            Balance = 0m,
            AdressLine1 = "1 Rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "FR"
        };
        var order = new Order
        {
            Id = 10,
            RestaurantId = 1,
            CustomerId = CustomerId,
            Restaurant = restaurant,
            TotalAmount = 100m,
            OriginalAmount = 100m,
            Status = OrderStatus.Ready,
            Items = []
        };
        var program = new LoyaltyProgram
        {
            Id = 1,
            RestaurantId = 1,
            IsActive = true,
            PointsPerEuro = 1.0m,
            EurosPerPoint = 0.10m
        };
        var account = new LoyaltyAccount
        {
            Id = 1,
            LoyaltyProgramId = 1,
            CustomerId = CustomerId,
            PointsBalance = 0
        };

        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);
        _loyaltyRepository.GetByRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(program);
        _loyaltyRepository.GetAccountAsync(1, CustomerId, Arg.Any<CancellationToken>()).Returns(account);

        var result = await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = nameof(OrderStatus.Delivered) });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(account.PointsBalance, Is.EqualTo(100));
        Assert.That(order.LoyaltyPointsEarned, Is.EqualTo(100));
        await _loyaltyRepository.Received(1).UpdateAccountAsync(account, Arg.Any<CancellationToken>());
        await _loyaltyRepository.Received(1).CreateTransactionAsync(
            Arg.Is<LoyaltyTransaction>(t =>
                t.Type == LoyaltyTransactionType.Earn &&
                t.Points == 100 &&
                t.OrderId == 10),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStatusAsync_WhenDelivered_WithNoLoyaltyProgram_NoPointsEarned()
    {
        var restaurant = new Restaurant
        {
            Id = 1,
            Name = "Test",
            Balance = 0m,
            AdressLine1 = "1 Rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "FR"
        };
        var order = new Order
        {
            Id = 10,
            RestaurantId = 1,
            CustomerId = CustomerId,
            Restaurant = restaurant,
            TotalAmount = 100m,
            OriginalAmount = 100m,
            Status = OrderStatus.Ready,
            Items = []
        };

        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);
        _loyaltyRepository.GetByRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns((LoyaltyProgram?)null);

        var result = await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = nameof(OrderStatus.Delivered) });

        Assert.That(result.IsSuccess, Is.True);
        await _loyaltyRepository.DidNotReceive().GetAccountAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _loyaltyRepository.DidNotReceive().UpdateAccountAsync(Arg.Any<LoyaltyAccount>(), Arg.Any<CancellationToken>());
        await _loyaltyRepository.DidNotReceive().CreateTransactionAsync(Arg.Any<LoyaltyTransaction>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStatusAsync_WhenDelivered_WithInactiveLoyaltyProgram_NoPointsEarned()
    {
        var restaurant = new Restaurant
        {
            Id = 1,
            Name = "Test",
            Balance = 0m,
            AdressLine1 = "1 Rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "FR"
        };
        var order = new Order
        {
            Id = 10,
            RestaurantId = 1,
            CustomerId = CustomerId,
            Restaurant = restaurant,
            TotalAmount = 100m,
            OriginalAmount = 100m,
            Status = OrderStatus.Ready,
            Items = []
        };
        var program = new LoyaltyProgram
        {
            Id = 1,
            RestaurantId = 1,
            IsActive = false,
            PointsPerEuro = 1.0m,
            EurosPerPoint = 0.10m
        };

        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);
        _loyaltyRepository.GetByRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(program);

        var result = await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = nameof(OrderStatus.Delivered) });

        Assert.That(result.IsSuccess, Is.True);
        await _loyaltyRepository.DidNotReceive().GetAccountAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _loyaltyRepository.DidNotReceive().UpdateAccountAsync(Arg.Any<LoyaltyAccount>(), Arg.Any<CancellationToken>());
        await _loyaltyRepository.DidNotReceive().CreateTransactionAsync(Arg.Any<LoyaltyTransaction>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStatusAsync_WhenDelivered_PointsBasedOnOriginalAmount()
    {
        var restaurant = new Restaurant
        {
            Id = 1,
            Name = "Test",
            Balance = 0m,
            AdressLine1 = "1 Rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "FR"
        };
        var order = new Order
        {
            Id = 10,
            RestaurantId = 1,
            CustomerId = CustomerId,
            Restaurant = restaurant,
            OriginalAmount = 200m,
            TotalAmount = 150m,
            Status = OrderStatus.Ready,
            Items = []
        };
        var program = new LoyaltyProgram
        {
            Id = 1,
            RestaurantId = 1,
            IsActive = true,
            PointsPerEuro = 2.0m,
            EurosPerPoint = 0.10m
        };
        var account = new LoyaltyAccount
        {
            Id = 1,
            LoyaltyProgramId = 1,
            CustomerId = CustomerId,
            PointsBalance = 0
        };

        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);
        _loyaltyRepository.GetByRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(program);
        _loyaltyRepository.GetAccountAsync(1, CustomerId, Arg.Any<CancellationToken>()).Returns(account);

        var result = await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = nameof(OrderStatus.Delivered) });

        Assert.That(result.IsSuccess, Is.True);
        // Points = floor(200 * 2.0) = 400 (based on OriginalAmount, not TotalAmount)
        Assert.That(account.PointsBalance, Is.EqualTo(400));
        Assert.That(order.LoyaltyPointsEarned, Is.EqualTo(400));
        await _loyaltyRepository.Received(1).CreateTransactionAsync(
            Arg.Is<LoyaltyTransaction>(t =>
                t.Type == LoyaltyTransactionType.Earn &&
                t.Points == 400),
            Arg.Any<CancellationToken>());
    }

    // ─── CreateFromCartAsync discount tests ────────────────────────────────

    [Test]
    public async Task CreateFromCartAsync_WithNoDiscounts_SetsOriginalAmountEqualToTotal()
    {
        SetupBaseCreateMocks();

        Order? capturedOrder = null;
        _orderRepository.CreateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOrder = callInfo.Arg<Order>();
                return capturedOrder;
            });

        var result = await _sut.CreateFromCartAsync(CustomerId, CreateBaseRequest());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(capturedOrder, Is.Not.Null);
        Assert.That(capturedOrder!.OriginalAmount, Is.EqualTo(70m));
        Assert.That(capturedOrder.TotalAmount, Is.EqualTo(70m));
        Assert.That(capturedOrder.DiscountAmount, Is.EqualTo(0m));
        Assert.That(capturedOrder.Discounts, Is.Empty);
    }

    [Test]
    public async Task CreateFromCartAsync_WithAutomaticPercentagePromotion_AppliesDiscount()
    {
        SetupBaseCreateMocks();

        _promotionRepository.GetActiveByRestaurantAsync(RestaurantId, Arg.Any<CancellationToken>())
            .Returns(new List<Promotion>
            {
                new()
                {
                    Id = 1, Name = "10% off", PromotionType = PromotionType.Automatic,
                    DiscountType = DiscountType.Percentage, DiscountValue = 10m,
                    IsActive = true, PromotionDishes = []
                }
            });

        Order? capturedOrder = null;
        _orderRepository.CreateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOrder = callInfo.Arg<Order>();
                return capturedOrder;
            });

        var result = await _sut.CreateFromCartAsync(CustomerId, CreateBaseRequest());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(capturedOrder, Is.Not.Null);
        Assert.That(capturedOrder!.OriginalAmount, Is.EqualTo(70m));
        Assert.That(capturedOrder.DiscountAmount, Is.EqualTo(7m)); // 10% of 70
        Assert.That(capturedOrder.TotalAmount, Is.EqualTo(63m));
        Assert.That(capturedOrder.Discounts, Has.Count.EqualTo(1));
        Assert.That(capturedOrder.Discounts[0].Source, Is.EqualTo(OrderDiscountSource.Promotion));
    }

    [Test]
    public async Task CreateFromCartAsync_WithThresholdPromotion_WhenMet_AppliesDiscount()
    {
        SetupBaseCreateMocks();

        _promotionRepository.GetActiveByRestaurantAsync(RestaurantId, Arg.Any<CancellationToken>())
            .Returns(new List<Promotion>
            {
                new()
                {
                    Id = 1, Name = "5 off over 50", PromotionType = PromotionType.Threshold,
                    DiscountType = DiscountType.FixedAmount, DiscountValue = 5m,
                    MinOrderAmount = 50m, IsActive = true, PromotionDishes = []
                }
            });

        Order? capturedOrder = null;
        _orderRepository.CreateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOrder = callInfo.Arg<Order>();
                return capturedOrder;
            });

        var result = await _sut.CreateFromCartAsync(CustomerId, CreateBaseRequest());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(capturedOrder!.OriginalAmount, Is.EqualTo(70m));
        Assert.That(capturedOrder.DiscountAmount, Is.EqualTo(5m));
        Assert.That(capturedOrder.TotalAmount, Is.EqualTo(65m));
    }

    [Test]
    public async Task CreateFromCartAsync_WithThresholdPromotion_WhenNotMet_SkipsDiscount()
    {
        SetupBaseCreateMocks();

        _promotionRepository.GetActiveByRestaurantAsync(RestaurantId, Arg.Any<CancellationToken>())
            .Returns(new List<Promotion>
            {
                new()
                {
                    Id = 1, Name = "5 off over 100", PromotionType = PromotionType.Threshold,
                    DiscountType = DiscountType.FixedAmount, DiscountValue = 5m,
                    MinOrderAmount = 100m, IsActive = true, PromotionDishes = []
                }
            });

        Order? capturedOrder = null;
        _orderRepository.CreateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOrder = callInfo.Arg<Order>();
                return capturedOrder;
            });

        var result = await _sut.CreateFromCartAsync(CustomerId, CreateBaseRequest());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(capturedOrder!.OriginalAmount, Is.EqualTo(70m));
        Assert.That(capturedOrder.DiscountAmount, Is.EqualTo(0m));
        Assert.That(capturedOrder.TotalAmount, Is.EqualTo(70m));
        Assert.That(capturedOrder.Discounts, Is.Empty);
    }

    [Test]
    public async Task CreateFromCartAsync_WithItemBasedPromotion_AppliesOnlyToMatchingDishes()
    {
        SetupBaseCreateMocks();

        // Promotion applies only to DishId=100 (Plat A, 2x20=40 subtotal)
        _promotionRepository.GetActiveByRestaurantAsync(RestaurantId, Arg.Any<CancellationToken>())
            .Returns(new List<Promotion>
            {
                new()
                {
                    Id = 1, Name = "20% on Plat A", PromotionType = PromotionType.ItemBased,
                    DiscountType = DiscountType.Percentage, DiscountValue = 20m,
                    IsActive = true,
                    PromotionDishes = [new PromotionDish { DishId = 100 }]
                }
            });

        Order? capturedOrder = null;
        _orderRepository.CreateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOrder = callInfo.Arg<Order>();
                return capturedOrder;
            });

        var result = await _sut.CreateFromCartAsync(CustomerId, CreateBaseRequest());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(capturedOrder!.OriginalAmount, Is.EqualTo(70m));
        // 20% of 40 (matching subtotal) = 8
        Assert.That(capturedOrder.DiscountAmount, Is.EqualTo(8m));
        Assert.That(capturedOrder.TotalAmount, Is.EqualTo(62m));
    }

    [Test]
    public async Task CreateFromCartAsync_WithValidDiscountCode_AppliesDiscount()
    {
        SetupBaseCreateMocks();

        var code = new DiscountCode
        {
            Id = 1,
            Code = "SAVE10",
            RestaurantId = RestaurantId,
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10m,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidUntil = DateTime.UtcNow.AddDays(1),
            MaxRedemptions = 100,
            CurrentRedemptions = 0,
            PerUserLimit = 1
        };
        _discountCodeRepository.GetByCodeAndRestaurantAsync("SAVE10", RestaurantId, Arg.Any<CancellationToken>())
            .Returns(code);
        _discountCodeRepository.GetRedemptionCountByUserAsync(1, CustomerId, Arg.Any<CancellationToken>())
            .Returns(0);
        _discountCodeRepository.UpdateAsync(Arg.Any<DiscountCode>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<DiscountCode>());

        Order? capturedOrder = null;
        _orderRepository.CreateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOrder = callInfo.Arg<Order>();
                return capturedOrder;
            });

        var result = await _sut.CreateFromCartAsync(CustomerId, CreateBaseRequest(discountCode: "SAVE10"));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(capturedOrder!.OriginalAmount, Is.EqualTo(70m));
        Assert.That(capturedOrder.DiscountAmount, Is.EqualTo(7m)); // 10% of 70
        Assert.That(capturedOrder.TotalAmount, Is.EqualTo(63m));
        Assert.That(capturedOrder.DiscountCodeId, Is.EqualTo(1));
        Assert.That(code.CurrentRedemptions, Is.EqualTo(1));
        await _discountCodeRepository.Received(1).CreateRedemptionAsync(
            Arg.Any<DiscountCodeRedemption>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateFromCartAsync_WithExpiredDiscountCode_ReturnsError()
    {
        SetupBaseCreateMocks();

        var code = new DiscountCode
        {
            Id = 1,
            Code = "EXPIRED",
            RestaurantId = RestaurantId,
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10m,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-10),
            ValidUntil = DateTime.UtcNow.AddDays(-1), // expired
            PerUserLimit = 1
        };
        _discountCodeRepository.GetByCodeAndRestaurantAsync("EXPIRED", RestaurantId, Arg.Any<CancellationToken>())
            .Returns(code);

        var result = await _sut.CreateFromCartAsync(CustomerId, CreateBaseRequest(discountCode: "EXPIRED"));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.DiscountCodeInvalid));
    }

    [Test]
    public async Task CreateFromCartAsync_WithMaxRedemptionsReached_ReturnsError()
    {
        SetupBaseCreateMocks();

        var code = new DiscountCode
        {
            Id = 1,
            Code = "MAXED",
            RestaurantId = RestaurantId,
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10m,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidUntil = DateTime.UtcNow.AddDays(1),
            MaxRedemptions = 5,
            CurrentRedemptions = 5,
            PerUserLimit = 10
        };
        _discountCodeRepository.GetByCodeAndRestaurantAsync("MAXED", RestaurantId, Arg.Any<CancellationToken>())
            .Returns(code);

        var result = await _sut.CreateFromCartAsync(CustomerId, CreateBaseRequest(discountCode: "MAXED"));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.DiscountCodeMaxRedemptions));
    }

    [Test]
    public async Task CreateFromCartAsync_WithLoyaltyPoints_AppliesDiscount()
    {
        SetupBaseCreateMocks();

        var program = new LoyaltyProgram
        {
            Id = 1,
            RestaurantId = RestaurantId,
            IsActive = true,
            EurosPerPoint = 0.10m,
            PointsPerEuro = 1m
        };
        var account = new LoyaltyAccount
        {
            Id = 1,
            LoyaltyProgramId = 1,
            CustomerId = CustomerId,
            PointsBalance = 100
        };
        _loyaltyRepository.GetByRestaurantAsync(RestaurantId, Arg.Any<CancellationToken>())
            .Returns(program);
        _loyaltyRepository.GetAccountAsync(1, CustomerId, Arg.Any<CancellationToken>())
            .Returns(account);

        Order? capturedOrder = null;
        _orderRepository.CreateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOrder = callInfo.Arg<Order>();
                return capturedOrder;
            });

        // Redeem 50 points = 50 * 0.10 = 5 euros
        var result = await _sut.CreateFromCartAsync(CustomerId, CreateBaseRequest(loyaltyPoints: 50));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(capturedOrder!.OriginalAmount, Is.EqualTo(70m));
        Assert.That(capturedOrder.DiscountAmount, Is.EqualTo(5m));
        Assert.That(capturedOrder.TotalAmount, Is.EqualTo(65m));
        Assert.That(capturedOrder.LoyaltyPointsUsed, Is.EqualTo(50));
        Assert.That(account.PointsBalance, Is.EqualTo(50)); // 100 - 50
        await _loyaltyRepository.Received(1).CreateTransactionAsync(
            Arg.Is<LoyaltyTransaction>(t =>
                t.Type == LoyaltyTransactionType.Redeem &&
                t.Points == -50),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateFromCartAsync_WithAllThreeStacking_AppliesAllDiscounts()
    {
        SetupBaseCreateMocks();

        // Promotion: 10% automatic = 7
        _promotionRepository.GetActiveByRestaurantAsync(RestaurantId, Arg.Any<CancellationToken>())
            .Returns(new List<Promotion>
            {
                new()
                {
                    Id = 1, Name = "10% off", PromotionType = PromotionType.Automatic,
                    DiscountType = DiscountType.Percentage, DiscountValue = 10m,
                    IsActive = true, PromotionDishes = []
                }
            });

        // Discount code: fixed 5
        var code = new DiscountCode
        {
            Id = 1,
            Code = "SAVE5",
            RestaurantId = RestaurantId,
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 5m,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidUntil = DateTime.UtcNow.AddDays(1),
            MaxRedemptions = 100,
            CurrentRedemptions = 0,
            PerUserLimit = 1
        };
        _discountCodeRepository.GetByCodeAndRestaurantAsync("SAVE5", RestaurantId, Arg.Any<CancellationToken>())
            .Returns(code);
        _discountCodeRepository.GetRedemptionCountByUserAsync(1, CustomerId, Arg.Any<CancellationToken>())
            .Returns(0);
        _discountCodeRepository.UpdateAsync(Arg.Any<DiscountCode>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<DiscountCode>());

        // Loyalty: 30 points * 0.10 = 3
        var program = new LoyaltyProgram
        {
            Id = 1,
            RestaurantId = RestaurantId,
            IsActive = true,
            EurosPerPoint = 0.10m,
            PointsPerEuro = 1m
        };
        var account = new LoyaltyAccount
        {
            Id = 1,
            LoyaltyProgramId = 1,
            CustomerId = CustomerId,
            PointsBalance = 100
        };
        _loyaltyRepository.GetByRestaurantAsync(RestaurantId, Arg.Any<CancellationToken>())
            .Returns(program);
        _loyaltyRepository.GetAccountAsync(1, CustomerId, Arg.Any<CancellationToken>())
            .Returns(account);

        Order? capturedOrder = null;
        _orderRepository.CreateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOrder = callInfo.Arg<Order>();
                return capturedOrder;
            });

        // Total discounts: 7 (promo) + 5 (code) + 3 (loyalty) = 15
        var result = await _sut.CreateFromCartAsync(CustomerId,
            CreateBaseRequest(discountCode: "SAVE5", loyaltyPoints: 30));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(capturedOrder!.OriginalAmount, Is.EqualTo(70m));
        Assert.That(capturedOrder.DiscountAmount, Is.EqualTo(15m));
        Assert.That(capturedOrder.TotalAmount, Is.EqualTo(55m));
        Assert.That(capturedOrder.Discounts, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task CreateFromCartAsync_DiscountCannotExceedOriginalAmount()
    {
        // Use low-priced items: total = 2*1 + 1*1 = 3
        var cart = CreateCartWithItems(price1: 1m, price2: 1m);
        SetupBaseCreateMocks(cart: cart);

        // Promotion: fixed 100 discount (way more than order total of 3)
        _promotionRepository.GetActiveByRestaurantAsync(RestaurantId, Arg.Any<CancellationToken>())
            .Returns(new List<Promotion>
            {
                new()
                {
                    Id = 1, Name = "100 off", PromotionType = PromotionType.Automatic,
                    DiscountType = DiscountType.FixedAmount, DiscountValue = 100m,
                    IsActive = true, PromotionDishes = []
                }
            });

        Order? capturedOrder = null;
        _orderRepository.CreateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOrder = callInfo.Arg<Order>();
                return capturedOrder;
            });

        var result = await _sut.CreateFromCartAsync(CustomerId, CreateBaseRequest());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(capturedOrder!.OriginalAmount, Is.EqualTo(3m));
        Assert.That(capturedOrder.DiscountAmount, Is.EqualTo(3m)); // capped at originalAmount
        Assert.That(capturedOrder.TotalAmount, Is.EqualTo(0m));
    }
}
