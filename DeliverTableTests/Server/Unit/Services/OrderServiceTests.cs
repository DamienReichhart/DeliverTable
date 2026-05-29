using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Hubs;
using DeliverTableServer.Hubs.Interfaces;
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Dtos.Payment;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Helpers;
using Microsoft.AspNetCore.SignalR;
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
    private IUserRepository _userRepository = null!;
    private IEmailJobService _emailJobService = null!;
    private IPaymentService _paymentService = null!;
    private IHubContext<OrderHub, IOrderHub> _orderHubContext = null!;
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
        _userRepository = Substitute.For<IUserRepository>();
        _emailJobService = Substitute.For<IEmailJobService>();
        _paymentService = Substitute.For<IPaymentService>();
        _orderHubContext = Substitute.For<IHubContext<OrderHub, IOrderHub>>();

        _appEnvironment = AppEnvironmentTestHelper.SetupEnvironment();

        _paymentService.CreateIntentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CreateIntentResult>.Success(
                new CreateIntentResult("pi_test_secret", "pi_test_id", 70m, "EUR")));

        // Default: any customerId resolves to a user with a complete billing address
        // so the CreateFromCartAsync billing-address guard passes. Tests that need
        // to exercise the guard's failure path override this mock explicitly.
        _userRepository.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>())
            .Returns(CreateValidUser());

        _sut = new OrderService(
            _orderRepository, _cartRepository, _restaurantRepository,
            _transactionRepository, _promotionRepository, _discountCodeRepository,
            _loyaltyRepository, _userRepository, _emailJobService, _paymentService,
            _orderHubContext, _appEnvironment);
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
        // CurrentRedemptions is incremented at payment-commit time (not at order creation),
        // so it stays 0 here.
        Assert.That(code.CurrentRedemptions, Is.EqualTo(0));
        await _discountCodeRepository.DidNotReceive().UpdateAsync(Arg.Any<DiscountCode>(), Arg.Any<CancellationToken>());
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
    public async Task CreateFromCart_WhenDiscountsExceedSubtotal_ScalesIndividualDiscountAmounts()
    {
        // Cart subtotal = 2*1 + 1*1 = 3. Promotion adds 2 (FixedAmount), DiscountCode adds 5 (FixedAmount).
        // Raw sum = 7 > 3, so each row must be proportionally scaled to land at exactly 3 in aggregate.
        var cart = CreateCartWithItems(price1: 1m, price2: 1m);
        SetupBaseCreateMocks(cart: cart);

        _promotionRepository.GetActiveByRestaurantAsync(RestaurantId, Arg.Any<CancellationToken>())
            .Returns(new List<Promotion>
            {
                new()
                {
                    Id = 1, Name = "2 off", PromotionType = PromotionType.Automatic,
                    DiscountType = DiscountType.FixedAmount, DiscountValue = 2m,
                    IsActive = true, PromotionDishes = []
                }
            });

        var code = new DiscountCode
        {
            Id = 9,
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
        _discountCodeRepository.GetRedemptionCountByUserAsync(9, CustomerId, Arg.Any<CancellationToken>())
            .Returns(0);

        Order? capturedOrder = null;
        _orderRepository.CreateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOrder = callInfo.Arg<Order>();
                return capturedOrder;
            });

        var result = await _sut.CreateFromCartAsync(CustomerId, CreateBaseRequest(discountCode: "SAVE5"));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(capturedOrder!.OriginalAmount, Is.EqualTo(3m));
        // Individual rows are scaled so the sum lands at exactly originalAmount.
        Assert.That(capturedOrder.Discounts.Sum(d => d.Amount), Is.EqualTo(capturedOrder.OriginalAmount));
        // DiscountAmount equals originalAmount (full coverage), TotalAmount falls to zero.
        Assert.That(capturedOrder.DiscountAmount, Is.EqualTo(capturedOrder.OriginalAmount));
        Assert.That(capturedOrder.TotalAmount, Is.EqualTo(0m));
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

    // ─── GuestCount / OrderType validation tests ─────────────────────────

    [Test]
    public async Task CreateFromCartAsync_DeliveryOrder_IgnoresGuestCountAndDefaultsToOne()
    {
        SetupBaseCreateMocks();

        var request = CreateBaseRequest();
        request.OrderType = nameof(OrderType.Delivery);
        request.GuestCount = 25; // should be ignored for delivery

        Order? capturedOrder = null;
        _orderRepository.CreateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOrder = callInfo.Arg<Order>();
                return capturedOrder;
            });

        var result = await _sut.CreateFromCartAsync(CustomerId, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(capturedOrder!.GuestCount, Is.EqualTo(1));
    }

    [Test]
    public async Task CreateFromCartAsync_DineInOrder_ValidatesGuestCount()
    {
        var request = CreateBaseRequest();
        request.OrderType = nameof(OrderType.DineIn);
        request.GuestCount = 0; // invalid

        var result = await _sut.CreateFromCartAsync(CustomerId, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.GuestCountRequired));
    }

    [Test]
    public async Task CreateFromCartAsync_DineInOrder_WithValidGuestCount_Succeeds()
    {
        SetupBaseCreateMocks();

        var request = CreateBaseRequest();
        request.OrderType = nameof(OrderType.DineIn);
        request.GuestCount = 4;
        request.DeliveryAddress = string.Empty; // not needed for dine-in

        Order? capturedOrder = null;
        _orderRepository.CreateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOrder = callInfo.Arg<Order>();
                return capturedOrder;
            });

        var result = await _sut.CreateFromCartAsync(CustomerId, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(capturedOrder!.GuestCount, Is.EqualTo(4));
    }

    [TestCase(0)]
    [TestCase(51)]
    public async Task CreateFromCartAsync_DineInOrder_WithInvalidGuestCount_ReturnsError(int guestCount)
    {
        var request = CreateBaseRequest();
        request.OrderType = nameof(OrderType.DineIn);
        request.GuestCount = guestCount;

        var result = await _sut.CreateFromCartAsync(CustomerId, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.GuestCountRequired));
    }

    // ─── New payment-flow tests ────────────────────────────────────────────

    [Test]
    public async Task CreateFromCartAsync_NewFlow_CreatesAwaitingPaymentAndReturnsClientSecret()
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
        Assert.That(capturedOrder!.Status, Is.EqualTo(OrderStatus.AwaitingPayment));
        Assert.That(capturedOrder.PaymentStatus, Is.EqualTo(PaymentStatus.Pending));
        Assert.That(result.Value!.ClientSecret, Is.EqualTo("pi_test_secret"));
        Assert.That(result.Value.PublishableKey, Is.EqualTo("pk_test_stripe"));
    }

    [Test]
    public async Task CreateFromCartAsync_NewFlow_DoesNotClearCart()
    {
        SetupBaseCreateMocks();

        await _sut.CreateFromCartAsync(CustomerId, CreateBaseRequest());

        await _cartRepository.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateFromCartAsync_NewFlow_DoesNotQueueConfirmationEmail()
    {
        SetupBaseCreateMocks();

        await _sut.CreateFromCartAsync(CustomerId, CreateBaseRequest());

        await _emailJobService.DidNotReceive().QueueOrderConfirmationAsync(
            Arg.Any<DeliverTableInfrastructure.Models.Order>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Test]
    public async Task CreateFromCartAsync_NewFlow_WhenPaymentServiceFails_ReturnsError()
    {
        SetupBaseCreateMocks();

        _paymentService.CreateIntentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CreateIntentResult>.Failure(
                new ServiceError("Stripe indisponible", 502)));

        var result = await _sut.CreateFromCartAsync(CustomerId, CreateBaseRequest());

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo("Stripe indisponible"));
    }

    [Test]
    public async Task CreateFromCartAsync_NewFlow_ReturnsCreateOrderResponseWithCorrectOrderId()
    {
        SetupBaseCreateMocks();

        _orderRepository.CreateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var o = callInfo.Arg<Order>();
                o.Id = 42;
                return o;
            });

        _paymentService.CreateIntentAsync(42, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CreateIntentResult>.Success(
                new CreateIntentResult("pi_secret_42", "pi_42", 70m, "EUR")));

        var result = await _sut.CreateFromCartAsync(CustomerId, CreateBaseRequest());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.OrderId, Is.EqualTo(42));
        Assert.That(result.Value.ClientSecret, Is.EqualTo("pi_secret_42"));
    }

    // ─── Billing-address guard tests ───────────────────────────────────────

    [Test]
    public async Task CreateFromCart_WhenBillingAddressIncomplete_ReturnsBillingError()
    {
        var customer = CreateValidUser("c@example.fr");
        customer.Id = 1;
        customer.BillingAddressLine1 = string.Empty; // wipe one required field

        _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(customer);

        var request = new CreateOrderRequest
        {
            RestaurantId = 5,
            OrderType = nameof(OrderType.Delivery),
            DeliveryAddress = "1 av des Champs-Élysées",
        };

        var result = await _sut.CreateFromCartAsync(1, request, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.BillingAddressIncomplete));
        await _restaurantRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Test]
    public async Task CreateFromCart_WhenBillingAddressComplete_PassesGuard()
    {
        var customer = CreateValidUser("c@example.fr");
        customer.Id = 1;
        // CreateValidUser already seeds a complete billing address.

        _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(customer);

        var request = new CreateOrderRequest
        {
            RestaurantId = 5,
            OrderType = nameof(OrderType.Delivery),
            DeliveryAddress = "1 av des Champs-Élysées",
        };

        var result = await _sut.CreateFromCartAsync(1, request, CancellationToken.None);

        // Guard MUST pass; downstream failures (RestaurantNotFound, etc.) are acceptable.
        Assert.That(result.IsSuccess
            || result.Error!.Message != ErrorMessages.BillingAddressIncomplete,
            Is.True);
        await _restaurantRepository.Received().GetByIdAsync(5, Arg.Any<CancellationToken>());
    }

    // ─── Payment integration tests for UpdateStatusAsync ────────────────────

    [Test]
    public async Task UpdateStatusAsync_PendingToConfirmed_CallsPaymentCapture()
    {
        var order = new Order { Id = 10, Status = OrderStatus.Pending, Restaurant = new Restaurant(), Items = [], Discounts = [] };
        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);
        _paymentService.CaptureAsync(10, Arg.Any<CancellationToken>()).Returns(ServiceResult.Success());

        var result = await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = nameof(OrderStatus.Confirmed) }, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(order.Status, Is.EqualTo(OrderStatus.Confirmed));
        await _paymentService.Received(1).CaptureAsync(10, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStatusAsync_CaptureFails_KeepsOrderPending()
    {
        var order = new Order { Id = 10, Status = OrderStatus.Pending, Restaurant = new Restaurant(), Items = [], Discounts = [] };
        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _paymentService.CaptureAsync(10, Arg.Any<CancellationToken>()).Returns(new ServiceError("fail"));

        var result = await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = nameof(OrderStatus.Confirmed) }, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(order.Status, Is.EqualTo(OrderStatus.Pending));
    }

    [Test]
    public async Task UpdateStatusAsync_PendingToRefused_CancelsAuthorization()
    {
        var order = new Order { Id = 10, Status = OrderStatus.Pending, Restaurant = new Restaurant(), Items = [], Discounts = [] };
        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);
        _paymentService.CancelAuthorizationAsync(10, Arg.Any<CancellationToken>()).Returns(ServiceResult.Success());

        await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = nameof(OrderStatus.Refused) }, CancellationToken.None);

        await _paymentService.Received(1).CancelAuthorizationAsync(10, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStatusAsync_CancellationAfterCapture_Refunds()
    {
        var order = new Order
        {
            Id = 10,
            Status = OrderStatus.Preparing,
            PaymentStatus = PaymentStatus.Completed,
            TotalAmount = 20m,
            Restaurant = new Restaurant(),
            Items = [],
            Discounts = []
        };
        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);
        _paymentService
            .RefundAsync(10, 20m, "order_cancelled", null, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefundDto>.Success(
                new RefundDto(1, 20m, "EUR", "order_cancelled", DateTime.UtcNow)));

        await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = nameof(OrderStatus.Cancelled) }, CancellationToken.None);

        await _paymentService.Received(1).RefundAsync(10, 20m, "order_cancelled", null, Arg.Any<CancellationToken>());
    }

    // ─── CancelOrderAsync payment-aware tests ──────────────────────────────

    [Test]
    public async Task CancelOrderAsync_OrderInPending_ReleasesStripeAuthorization()
    {
        var order = new Order
        {
            Id = 10,
            CustomerId = CustomerId,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Authorized,
            TotalAmount = 25m,
            Restaurant = new Restaurant(),
            Items = [],
            Discounts = []
        };
        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);
        _paymentService.CancelAuthorizationAsync(10, Arg.Any<CancellationToken>()).Returns(ServiceResult.Success());

        var result = await _sut.CancelOrderAsync(10, CustomerId, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(order.Status, Is.EqualTo(OrderStatus.Cancelled));
        await _paymentService.Received(1).CancelAuthorizationAsync(10, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelOrderAsync_OrderInPreparing_RefundsStripe()
    {
        var order = new Order
        {
            Id = 10,
            CustomerId = CustomerId,
            Status = OrderStatus.Preparing,
            PaymentStatus = PaymentStatus.Completed,
            TotalAmount = 35m,
            Restaurant = new Restaurant(),
            Items = [],
            Discounts = []
        };
        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);
        _paymentService
            .RefundAsync(10, 35m, "order_cancelled", null, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefundDto>.Success(
                new RefundDto(1, 35m, "EUR", "order_cancelled", DateTime.UtcNow)));

        var result = await _sut.CancelOrderAsync(10, CustomerId, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(order.Status, Is.EqualTo(OrderStatus.Cancelled));
        await _paymentService.Received(1).RefundAsync(10, 35m, "order_cancelled", null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelOrderAsync_WrongCustomer_ReturnsNotFound()
    {
        var order = new Order
        {
            Id = 10,
            CustomerId = 999,
            Status = OrderStatus.Pending,
            Restaurant = new Restaurant(),
            Items = [],
            Discounts = []
        };
        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.CancelOrderAsync(10, CustomerId, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.OrderNotFound));
        await _paymentService.DidNotReceive().CancelAuthorizationAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _paymentService.DidNotReceive().RefundAsync(Arg.Any<int>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelOrderAsync_AlreadyCancelled_ReturnsError()
    {
        var order = new Order
        {
            Id = 10,
            CustomerId = CustomerId,
            Status = OrderStatus.Cancelled,
            Restaurant = new Restaurant(),
            Items = [],
            Discounts = []
        };
        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.CancelOrderAsync(10, CustomerId, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.OrderCannotBeCancelled));
    }

    // ─── DeliveredAt tests ─────────────────────────────────────────────────

    [Test]
    public async Task UpdateStatusAsync_WhenDelivered_SetsDeliveredAtToUtcNow()
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
            TotalAmount = 100m,
            Status = OrderStatus.Ready,
            DeliveredAt = null,
            Items = []
        };
        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);

        var before = DateTime.UtcNow;
        var result = await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = nameof(OrderStatus.Delivered) });
        var after = DateTime.UtcNow;

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(order.DeliveredAt, Is.Not.Null);
        Assert.That(order.DeliveredAt!.Value, Is.GreaterThanOrEqualTo(before));
        Assert.That(order.DeliveredAt!.Value, Is.LessThanOrEqualTo(after));
        Assert.That(order.DeliveredAt!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public async Task UpdateStatusAsync_WhenNotDelivered_DoesNotSetDeliveredAt()
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
            TotalAmount = 100m,
            Status = OrderStatus.Confirmed,
            DeliveredAt = null,
            Items = []
        };
        _orderRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(order, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.UpdateStatusAsync(10, new UpdateOrderStatusRequest { Status = nameof(OrderStatus.Preparing) });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(order.DeliveredAt, Is.Null);
    }
}
