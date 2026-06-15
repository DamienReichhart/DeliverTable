using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;
using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class AdminTransactionServiceTests
{
    private ITransactionRepository _transactionRepository = null!;
    private AdminTransactionService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _sut = new AdminTransactionService(_transactionRepository);
    }

    #region GetAllAsync

    [Test]
    public async Task GetAllAsync_ReturnsAllTransactions()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        List<RestaurantTransaction> transactions = new List<RestaurantTransaction>
        {
            new()
            {
                Id = 1, RestaurantId = 1, Restaurant = restaurant,
                Type = TransactionType.Credit, GrossAmount = 100m,
                CommissionAmount = 10m, NetAmount = 90m, BalanceAfter = 90m
            },
            new()
            {
                Id = 2, RestaurantId = 1, Restaurant = restaurant,
                Type = TransactionType.Withdrawal, GrossAmount = 50m,
                CommissionAmount = 0m, NetAmount = 50m, BalanceAfter = 40m
            }
        };

        _transactionRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(transactions);

        ServiceResult<List<AdminTransactionResponse>> result = await _sut.GetAllAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAllAsync_MapsFieldsCorrectly()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        List<RestaurantTransaction> transactions = new List<RestaurantTransaction>
        {
            new()
            {
                Id = 1, RestaurantId = 1, Restaurant = restaurant,
                Type = TransactionType.Credit, GrossAmount = 100m,
                CommissionAmount = 10m, NetAmount = 90m, BalanceAfter = 90m, OrderId = 42
            }
        };

        _transactionRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(transactions);

        ServiceResult<List<AdminTransactionResponse>> result = await _sut.GetAllAsync();

        Assert.That(result.IsSuccess, Is.True);
        AdminTransactionResponse dto = result.Value![0];
        Assert.That(dto.Id, Is.EqualTo(1));
        Assert.That(dto.Type, Is.EqualTo(nameof(TransactionType.Credit)));
        Assert.That(dto.GrossAmount, Is.EqualTo(100m));
        Assert.That(dto.CommissionAmount, Is.EqualTo(10m));
        Assert.That(dto.NetAmount, Is.EqualTo(90m));
        Assert.That(dto.BalanceAfter, Is.EqualTo(90m));
        Assert.That(dto.RestaurantName, Is.EqualTo(restaurant.Name));
        Assert.That(dto.OrderId, Is.EqualTo(42));
    }

    #endregion

    #region GetByIdAsync

    [Test]
    public async Task GetByIdAsync_WhenExists_ReturnsTransaction()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        RestaurantTransaction transaction = new RestaurantTransaction
        {
            Id = 1,
            RestaurantId = 1,
            Restaurant = restaurant,
            Type = TransactionType.Credit,
            GrossAmount = 100m,
            CommissionAmount = 10m,
            NetAmount = 90m,
            BalanceAfter = 90m
        };

        _transactionRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(transaction);

        ServiceResult<AdminTransactionResponse> result = await _sut.GetByIdAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(1));
        Assert.That(result.Value.RestaurantName, Is.EqualTo(restaurant.Name));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotExists_Returns404()
    {
        _transactionRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((RestaurantTransaction?)null);

        ServiceResult<AdminTransactionResponse> result = await _sut.GetByIdAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.TransactionNotFound));
    }

    #endregion
}
