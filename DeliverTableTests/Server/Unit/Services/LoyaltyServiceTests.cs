using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Loyalty;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;
using DeliverTableServer.Common;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class LoyaltyServiceTests
{
    private ILoyaltyRepository _loyaltyRepository = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private LoyaltyService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _loyaltyRepository = Substitute.For<ILoyaltyRepository>();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _sut = new LoyaltyService(_loyaltyRepository, _restaurantRepository);
    }

    // --- CreateOrUpdateProgramAsync ---

    [Test]
    public async Task CreateOrUpdateProgramAsync_WhenNoProgramExists_CreatesNew()
    {
        Restaurant restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _loyaltyRepository.GetByRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns((LoyaltyProgram?)null);

        CreateLoyaltyProgramRequest request = new CreateLoyaltyProgramRequest { PointsPerEuro = 2.0m, EurosPerPoint = 0.05m };

        _loyaltyRepository.CreateAsync(Arg.Any<LoyaltyProgram>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                LoyaltyProgram p = callInfo.ArgAt<LoyaltyProgram>(0);
                p.Id = 10;
                return p;
            });

        ServiceResult<LoyaltyProgramDto> result = await _sut.CreateOrUpdateProgramAsync(1, 1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(10));
        Assert.That(result.Value.PointsPerEuro, Is.EqualTo(2.0m));
        Assert.That(result.Value.EurosPerPoint, Is.EqualTo(0.05m));
        Assert.That(result.Value.RestaurantId, Is.EqualTo(1));
    }

    [Test]
    public async Task CreateOrUpdateProgramAsync_WhenProgramExists_Updates()
    {
        Restaurant restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        LoyaltyProgram existing = new LoyaltyProgram
        {
            Id = 5,
            RestaurantId = 1,
            PointsPerEuro = 1.0m,
            EurosPerPoint = 0.10m
        };
        _loyaltyRepository.GetByRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        _loyaltyRepository.UpdateAsync(Arg.Any<LoyaltyProgram>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<LoyaltyProgram>(0));

        CreateLoyaltyProgramRequest request = new CreateLoyaltyProgramRequest { PointsPerEuro = 3.0m, EurosPerPoint = 0.02m };

        ServiceResult<LoyaltyProgramDto> result = await _sut.CreateOrUpdateProgramAsync(1, 1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(5));
        Assert.That(result.Value.PointsPerEuro, Is.EqualTo(3.0m));
        Assert.That(result.Value.EurosPerPoint, Is.EqualTo(0.02m));
    }

    [Test]
    public async Task CreateOrUpdateProgramAsync_WhenNotOwner_ReturnsError()
    {
        Restaurant restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        CreateLoyaltyProgramRequest request = new CreateLoyaltyProgramRequest { PointsPerEuro = 1.0m, EurosPerPoint = 0.10m };

        ServiceResult<LoyaltyProgramDto> result = await _sut.CreateOrUpdateProgramAsync(1, 999, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RestaurantNotFound));
    }

    [Test]
    public async Task CreateOrUpdateProgramAsync_WhenRestaurantNotFound_ReturnsError()
    {
        _restaurantRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        CreateLoyaltyProgramRequest request = new CreateLoyaltyProgramRequest { PointsPerEuro = 1.0m, EurosPerPoint = 0.10m };

        ServiceResult<LoyaltyProgramDto> result = await _sut.CreateOrUpdateProgramAsync(99, 1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RestaurantNotFound));
    }

    // --- GetProgramAsync ---

    [Test]
    public async Task GetProgramAsync_WhenExists_ReturnsProgram()
    {
        LoyaltyProgram program = new LoyaltyProgram
        {
            Id = 7,
            RestaurantId = 1,
            PointsPerEuro = 1.5m,
            EurosPerPoint = 0.08m,
            IsActive = true
        };
        _loyaltyRepository.GetByRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(program);

        ServiceResult<LoyaltyProgramDto> result = await _sut.GetProgramAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(7));
        Assert.That(result.Value.PointsPerEuro, Is.EqualTo(1.5m));
        Assert.That(result.Value.IsActive, Is.True);
    }

    [Test]
    public async Task GetProgramAsync_WhenNotFound_ReturnsError()
    {
        _loyaltyRepository.GetByRestaurantAsync(99, Arg.Any<CancellationToken>()).Returns((LoyaltyProgram?)null);

        ServiceResult<LoyaltyProgramDto> result = await _sut.GetProgramAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.LoyaltyProgramNotFound));
    }

    // --- GetMyAccountAsync ---

    [Test]
    public async Task GetMyAccountAsync_WhenAccountExists_ReturnsAccount()
    {
        LoyaltyProgram program = new LoyaltyProgram
        {
            Id = 3,
            RestaurantId = 1,
            PointsPerEuro = 1.0m,
            EurosPerPoint = 0.10m
        };
        _loyaltyRepository.GetByRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(program);

        LoyaltyAccount account = new LoyaltyAccount
        {
            Id = 20,
            LoyaltyProgramId = 3,
            CustomerId = 5,
            PointsBalance = 100
        };
        _loyaltyRepository.GetAccountAsync(3, 5, Arg.Any<CancellationToken>()).Returns(account);

        ServiceResult<LoyaltyAccountDto> result = await _sut.GetMyAccountAsync(1, 5);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(20));
        Assert.That(result.Value.PointsBalance, Is.EqualTo(100));
        Assert.That(result.Value.EuroEquivalent, Is.EqualTo(10.0m));
    }

    [Test]
    public async Task GetMyAccountAsync_WhenNoAccount_CreatesAndReturnsNewAccount()
    {
        LoyaltyProgram program = new LoyaltyProgram
        {
            Id = 3,
            RestaurantId = 1,
            PointsPerEuro = 1.0m,
            EurosPerPoint = 0.10m
        };
        _loyaltyRepository.GetByRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(program);
        _loyaltyRepository.GetAccountAsync(3, 5, Arg.Any<CancellationToken>()).Returns((LoyaltyAccount?)null);

        _loyaltyRepository.CreateAccountAsync(Arg.Any<LoyaltyAccount>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                LoyaltyAccount a = callInfo.ArgAt<LoyaltyAccount>(0);
                a.Id = 30;
                return a;
            });

        ServiceResult<LoyaltyAccountDto> result = await _sut.GetMyAccountAsync(1, 5);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(30));
        Assert.That(result.Value.PointsBalance, Is.EqualTo(0));
        Assert.That(result.Value.EuroEquivalent, Is.EqualTo(0m));
    }

    [Test]
    public async Task GetMyAccountAsync_WhenNoProgramExists_ReturnsError()
    {
        _loyaltyRepository.GetByRestaurantAsync(99, Arg.Any<CancellationToken>()).Returns((LoyaltyProgram?)null);

        ServiceResult<LoyaltyAccountDto> result = await _sut.GetMyAccountAsync(99, 5);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.LoyaltyProgramNotFound));
    }
}
