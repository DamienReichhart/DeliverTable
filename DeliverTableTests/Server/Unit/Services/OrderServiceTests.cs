using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class OrderServiceTests
{
    private IOrderRepository _orderRepository = null!;
    private ICartRepository _cartRepository = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private IRestaurantTransactionRepository _transactionRepository = null!;
    private AppEnvironment _appEnvironment = null!;
    private OrderService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _orderRepository = Substitute.For<IOrderRepository>();
        _cartRepository = Substitute.For<ICartRepository>();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _transactionRepository = Substitute.For<IRestaurantTransactionRepository>();

        Environment.SetEnvironmentVariable("CONNECTION_STRING_DATABASE", "Host=localhost;Database=test");
        Environment.SetEnvironmentVariable("JWT_KEY", "TestKeyThatIsLongEnoughForHmacSha256Signing!");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "TestIssuer");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "TestAudience");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_SERVICE_URL", "http://localhost:3900");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_ACCESS_KEY", "key");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_SECRET_KEY", "secret");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_BUCKET_NAME", "bucket");
        Environment.SetEnvironmentVariable("PLATFORM_COMMISSION_RATE", "0.10");
        _appEnvironment = AppEnvironment.Load();

        _sut = new OrderService(_orderRepository, _cartRepository, _restaurantRepository, _transactionRepository, _appEnvironment);
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("CONNECTION_STRING_DATABASE", null);
        Environment.SetEnvironmentVariable("JWT_KEY", null);
        Environment.SetEnvironmentVariable("JWT_ISSUER", null);
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_SERVICE_URL", null);
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_ACCESS_KEY", null);
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_SECRET_KEY", null);
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_BUCKET_NAME", null);
        Environment.SetEnvironmentVariable("PLATFORM_COMMISSION_RATE", null);
    }

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
}
