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
public class AdminLoyaltyServiceTests
{
    private ILoyaltyRepository _loyaltyRepository = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private AdminLoyaltyService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _loyaltyRepository = Substitute.For<ILoyaltyRepository>();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _sut = new AdminLoyaltyService(_loyaltyRepository, _restaurantRepository);
    }

    #region GetAllProgramsAsync

    [Test]
    public async Task GetAllProgramsAsync_ReturnsAllPrograms()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var programs = new List<LoyaltyProgram>
        {
            new() { Id = 1, RestaurantId = 1, Restaurant = restaurant, Accounts = [new() { Id = 1 }] },
            new() { Id = 2, RestaurantId = 1, Restaurant = restaurant, Accounts = [] }
        };

        _loyaltyRepository.GetAllProgramsUnscopedAsync(Arg.Any<CancellationToken>()).Returns(programs);

        var result = await _sut.GetAllProgramsAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
        Assert.That(result.Value![0].AccountCount, Is.EqualTo(1));
        Assert.That(result.Value[1].AccountCount, Is.EqualTo(0));
    }

    #endregion

    #region GetProgramByIdAsync

    [Test]
    public async Task GetProgramByIdAsync_WhenExists_ReturnsProgram()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var program = CreateLoyaltyProgram(id: 1, restaurantId: 1);
        program.Restaurant = restaurant;
        program.Accounts = [new() { Id = 1 }, new() { Id = 2 }];

        _loyaltyRepository.GetProgramByIdWithAccountsAsync(1, Arg.Any<CancellationToken>()).Returns(program);

        var result = await _sut.GetProgramByIdAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(1));
        Assert.That(result.Value.RestaurantName, Is.EqualTo(restaurant.Name));
        Assert.That(result.Value.AccountCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetProgramByIdAsync_WhenNotExists_Returns404()
    {
        _loyaltyRepository.GetProgramByIdWithAccountsAsync(99, Arg.Any<CancellationToken>())
            .Returns((LoyaltyProgram?)null);

        var result = await _sut.GetProgramByIdAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.LoyaltyProgramNotFound));
    }

    #endregion

    #region CreateProgramAsync

    [Test]
    public async Task CreateProgramAsync_WhenRestaurantExists_CreatesProgram()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _loyaltyRepository.GetByRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns((LoyaltyProgram?)null);
        _loyaltyRepository.CreateAsync(Arg.Any<LoyaltyProgram>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var p = callInfo.Arg<LoyaltyProgram>();
                p.Id = 10;
                p.Restaurant = restaurant;
                return p;
            });

        var request = new AdminCreateLoyaltyProgramRequest
        {
            PointsPerEuro = 2.0m,
            EurosPerPoint = 0.05m,
            RestaurantId = 1,
            IsActive = true
        };

        var result = await _sut.CreateProgramAsync(request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.PointsPerEuro, Is.EqualTo(2.0m));
        Assert.That(result.Value.EurosPerPoint, Is.EqualTo(0.05m));
        Assert.That(result.Value.RestaurantName, Is.EqualTo(restaurant.Name));
    }

    [Test]
    public async Task CreateProgramAsync_WhenRestaurantNotExists_Returns404()
    {
        _restaurantRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        var request = new AdminCreateLoyaltyProgramRequest
        {
            PointsPerEuro = 1.0m,
            EurosPerPoint = 0.10m,
            RestaurantId = 99
        };

        var result = await _sut.CreateProgramAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RestaurantNotFound));
    }

    [Test]
    public async Task CreateProgramAsync_WhenProgramAlreadyExists_Returns400()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _loyaltyRepository.GetByRestaurantAsync(1, Arg.Any<CancellationToken>())
            .Returns(CreateLoyaltyProgram(id: 1, restaurantId: 1));

        var request = new AdminCreateLoyaltyProgramRequest
        {
            PointsPerEuro = 1.0m,
            EurosPerPoint = 0.10m,
            RestaurantId = 1
        };

        var result = await _sut.CreateProgramAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.LoyaltyProgramAlreadyExists));
    }

    #endregion

    #region UpdateProgramAsync

    [Test]
    public async Task UpdateProgramAsync_WhenExists_UpdatesAndReturns()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var program = CreateLoyaltyProgram(id: 1, restaurantId: 1);
        program.Restaurant = restaurant;
        program.Accounts = [];

        _loyaltyRepository.GetProgramByIdWithAccountsAsync(1, Arg.Any<CancellationToken>()).Returns(program);
        _loyaltyRepository.UpdateAsync(Arg.Any<LoyaltyProgram>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<LoyaltyProgram>());

        var request = new AdminUpdateLoyaltyProgramRequest
        {
            PointsPerEuro = 3.0m,
            EurosPerPoint = 0.02m,
            IsActive = false
        };

        var result = await _sut.UpdateProgramAsync(1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.PointsPerEuro, Is.EqualTo(3.0m));
        Assert.That(result.Value.EurosPerPoint, Is.EqualTo(0.02m));
        Assert.That(result.Value.IsActive, Is.False);
    }

    [Test]
    public async Task UpdateProgramAsync_WhenNotExists_Returns404()
    {
        _loyaltyRepository.GetProgramByIdWithAccountsAsync(99, Arg.Any<CancellationToken>())
            .Returns((LoyaltyProgram?)null);

        var request = new AdminUpdateLoyaltyProgramRequest
        {
            PointsPerEuro = 1.0m,
            EurosPerPoint = 0.10m
        };

        var result = await _sut.UpdateProgramAsync(99, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.LoyaltyProgramNotFound));
    }

    #endregion

    #region DeleteProgramAsync

    [Test]
    public async Task DeleteProgramAsync_WhenExists_ReturnsSuccess()
    {
        _loyaltyRepository.DeleteProgramAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteProgramAsync(1);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DeleteProgramAsync_WhenNotExists_Returns404()
    {
        _loyaltyRepository.DeleteProgramAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.DeleteProgramAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.LoyaltyProgramNotFound));
    }

    #endregion

    #region GetAccountsAsync

    [Test]
    public async Task GetAccountsAsync_WhenProgramExists_ReturnsAccounts()
    {
        var program = CreateLoyaltyProgram(id: 1, restaurantId: 1);
        program.Accounts = [];
        _loyaltyRepository.GetProgramByIdWithAccountsAsync(1, Arg.Any<CancellationToken>()).Returns(program);

        var customer = CreateValidUser();
        customer.FirstName = "Jean";
        customer.LastName = "Dupont";
        var accounts = new List<LoyaltyAccount>
        {
            new() { Id = 1, LoyaltyProgramId = 1, CustomerId = 1, Customer = customer, PointsBalance = 100 },
            new() { Id = 2, LoyaltyProgramId = 1, CustomerId = 2, Customer = customer, PointsBalance = 50 }
        };

        _loyaltyRepository.GetAccountsByProgramIdAsync(1, Arg.Any<CancellationToken>()).Returns(accounts);

        var result = await _sut.GetAccountsAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
        Assert.That(result.Value![0].CustomerName, Is.EqualTo("Jean Dupont"));
    }

    [Test]
    public async Task GetAccountsAsync_WhenProgramNotExists_Returns404()
    {
        _loyaltyRepository.GetProgramByIdWithAccountsAsync(99, Arg.Any<CancellationToken>())
            .Returns((LoyaltyProgram?)null);

        var result = await _sut.GetAccountsAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.LoyaltyProgramNotFound));
    }

    #endregion

    #region GetTransactionsAsync

    [Test]
    public async Task GetTransactionsAsync_ReturnsTransactions()
    {
        var transactions = new List<LoyaltyTransaction>
        {
            new() { Id = 1, LoyaltyAccountId = 1, Type = LoyaltyTransactionType.Earn, Points = 10 },
            new() { Id = 2, LoyaltyAccountId = 1, Type = LoyaltyTransactionType.Redeem, Points = -5, OrderId = 42 }
        };

        _loyaltyRepository.GetTransactionsByAccountIdAsync(1, Arg.Any<CancellationToken>()).Returns(transactions);

        var result = await _sut.GetTransactionsAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
        Assert.That(result.Value![0].Type, Is.EqualTo(LoyaltyTransactionType.Earn));
        Assert.That(result.Value[1].OrderId, Is.EqualTo(42));
    }

    #endregion
}
