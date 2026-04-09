using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;
using DeliverTableTests.Global.Helpers;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;

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

        _appEnvironment = AppEnvironmentTestHelper.SetupEnvironment();

        _sut = new RestaurantAccountService(_restaurantRepository, _transactionRepository, _appEnvironment);
    }

    [TearDown]
    public void TearDown() => AppEnvironmentTestHelper.CleanupEnvironment();

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

}
