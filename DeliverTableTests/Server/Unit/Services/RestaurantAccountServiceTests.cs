using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class RestaurantAccountServiceTests
{
    private IRestaurantRepository _restaurantRepository = null!;
    private IRestaurantTransactionRepository _transactionRepository = null!;
    private AppEnvironment _appEnvironment = null!;
    private RestaurantAccountService _sut = null!;

    [SetUp]
    public void SetUp()
    {
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

        _sut = new RestaurantAccountService(_restaurantRepository, _transactionRepository, _appEnvironment);
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
    public async Task GetAccountAsync_WhenRestaurantNotFound_ReturnsError()
    {
        _restaurantRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        var result = await _sut.GetAccountAsync(99, 1, new TransactionQuery());

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task GetAccountAsync_WhenNotOwner_ReturnsError()
    {
        var restaurant = CreateRestaurant(ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        var result = await _sut.GetAccountAsync(1, 999, new TransactionQuery());

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task GetAccountAsync_WhenOwner_ReturnsAccountWithBalance()
    {
        var restaurant = CreateRestaurant(ownerId: 5, balance: 360m);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _transactionRepository.GetByRestaurantAsync(1, Arg.Any<TransactionQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<RestaurantTransaction>(), 0));

        var result = await _sut.GetAccountAsync(1, 5, new TransactionQuery());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Balance, Is.EqualTo(360m));
    }

    [Test]
    public async Task WithdrawAsync_WhenRestaurantNotFound_ReturnsError()
    {
        _restaurantRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        var result = await _sut.WithdrawAsync(99, 1, new WithdrawRequest { Amount = 100 });

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task WithdrawAsync_WhenNotOwner_ReturnsError()
    {
        var restaurant = CreateRestaurant(ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        var result = await _sut.WithdrawAsync(1, 999, new WithdrawRequest { Amount = 100 });

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task WithdrawAsync_WhenInsufficientBalance_ReturnsError()
    {
        var restaurant = CreateRestaurant(ownerId: 5, balance: 50m);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        var result = await _sut.WithdrawAsync(1, 5, new WithdrawRequest { Amount = 100 });

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.InsufficientBalance));
    }

    [Test]
    public async Task WithdrawAsync_WhenValid_DecreasesBalanceAndCreatesTransaction()
    {
        var restaurant = CreateRestaurant(ownerId: 5, balance: 500m);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _transactionRepository.GetByRestaurantAsync(1, Arg.Any<TransactionQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<RestaurantTransaction>(), 0));

        var result = await _sut.WithdrawAsync(1, 5, new WithdrawRequest { Amount = 200 });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Balance, Is.EqualTo(300m));
        await _transactionRepository.Received(1).CreateAsync(
            Arg.Is<RestaurantTransaction>(t =>
                t.Type == DeliverTableSharedLibrary.Enums.TransactionType.Withdrawal &&
                t.NetAmount == 200m &&
                t.BalanceAfter == 300m),
            Arg.Any<CancellationToken>());
    }

    private static Restaurant CreateRestaurant(int ownerId, decimal balance = 0m)
    {
        return new Restaurant
        {
            Id = 1,
            Name = "Test Restaurant",
            OwnerId = ownerId,
            Balance = balance,
            AdressLine1 = "1 Rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "FR"
        };
    }
}
