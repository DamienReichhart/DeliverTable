using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.DiscountCode;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;
using DiscountCodeEntity = DeliverTableInfrastructure.Models.DiscountCode;
using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class DiscountCodeServiceTests
{
    private IDiscountCodeRepository _discountCodeRepository = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private DiscountCodeService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _discountCodeRepository = Substitute.For<IDiscountCodeRepository>();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _sut = new DiscountCodeService(_discountCodeRepository, _restaurantRepository);
    }

    [Test]
    public async Task CreateAsync_WithValidCode_ReturnsSuccess()
    {
        Restaurant restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        _discountCodeRepository.GetByCodeAndRestaurantAsync("SUMMER10", 1, Arg.Any<CancellationToken>())
            .Returns((DiscountCodeEntity?)null);

        CreateDiscountCodeRequest request = new CreateDiscountCodeRequest
        {
            Code = "SUMMER10",
            Description = "Réduction été",
            DiscountType = nameof(DiscountType.Percentage),
            DiscountValue = 10m,
            ValidFrom = new DateTime(2026, 6, 1),
            ValidUntil = new DateTime(2026, 8, 31),
            PerUserLimit = 1
        };

        _discountCodeRepository.CreateAsync(Arg.Any<DiscountCodeEntity>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                DiscountCodeEntity c = callInfo.ArgAt<DiscountCodeEntity>(0);
                c.Id = 42;
                return c;
            });

        ServiceResult<DiscountCodeDto> result = await _sut.CreateAsync(1, 1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(42));
        Assert.That(result.Value.Code, Is.EqualTo("SUMMER10"));
        Assert.That(result.Value.DiscountType, Is.EqualTo(nameof(DiscountType.Percentage)));
        Assert.That(result.Value.DiscountValue, Is.EqualTo(10m));
    }

    [Test]
    public async Task CreateAsync_WhenRestaurantNotFound_ReturnsError()
    {
        _restaurantRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        CreateDiscountCodeRequest request = new CreateDiscountCodeRequest
        {
            Code = "CODE1",
            DiscountType = nameof(DiscountType.Percentage),
            DiscountValue = 10m,
            ValidFrom = new DateTime(2026, 6, 1),
            ValidUntil = new DateTime(2026, 8, 31)
        };

        ServiceResult<DiscountCodeDto> result = await _sut.CreateAsync(99, 1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RestaurantNotFound));
    }

    [Test]
    public async Task CreateAsync_WhenNotOwner_ReturnsError()
    {
        Restaurant restaurant = CreateRestaurant(ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        CreateDiscountCodeRequest request = new CreateDiscountCodeRequest
        {
            Code = "CODE1",
            DiscountType = nameof(DiscountType.Percentage),
            DiscountValue = 10m,
            ValidFrom = new DateTime(2026, 6, 1),
            ValidUntil = new DateTime(2026, 8, 31)
        };

        ServiceResult<DiscountCodeDto> result = await _sut.CreateAsync(1, 999, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task CreateAsync_WithDuplicateCode_ReturnsError()
    {
        Restaurant restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        DiscountCodeEntity existingCode = new DiscountCodeEntity
        {
            Id = 10,
            RestaurantId = 1,
            Code = "DUP",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 5m,
            ValidFrom = new DateTime(2026, 1, 1),
            ValidUntil = new DateTime(2026, 12, 31)
        };
        _discountCodeRepository.GetByCodeAndRestaurantAsync("DUP", 1, Arg.Any<CancellationToken>())
            .Returns(existingCode);

        CreateDiscountCodeRequest request = new CreateDiscountCodeRequest
        {
            Code = "DUP",
            DiscountType = nameof(DiscountType.Percentage),
            DiscountValue = 10m,
            ValidFrom = new DateTime(2026, 6, 1),
            ValidUntil = new DateTime(2026, 8, 31)
        };

        ServiceResult<DiscountCodeDto> result = await _sut.CreateAsync(1, 1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.DiscountCodeAlreadyExists));
    }

    [Test]
    public async Task CreateAsync_WithInvalidDates_ReturnsError()
    {
        Restaurant restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        CreateDiscountCodeRequest request = new CreateDiscountCodeRequest
        {
            Code = "CODE1",
            DiscountType = nameof(DiscountType.Percentage),
            DiscountValue = 10m,
            ValidFrom = new DateTime(2026, 8, 31),
            ValidUntil = new DateTime(2026, 6, 1)
        };

        ServiceResult<DiscountCodeDto> result = await _sut.CreateAsync(1, 1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Message, Is.EqualTo(ErrorMessages.InvalidPromotionDates));
    }

    [Test]
    public async Task GetByRestaurantAsync_WhenOwner_ReturnsPaginatedResult()
    {
        Restaurant restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        List<DiscountCodeEntity> codes = new List<DiscountCodeEntity>
        {
            new()
            {
                Id = 1,
                RestaurantId = 1,
                Code = "CODE1",
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10m,
                ValidFrom = new DateTime(2026, 6, 1),
                ValidUntil = new DateTime(2026, 8, 31)
            }
        };

        _discountCodeRepository.GetByRestaurantAsync(1, Arg.Any<DiscountCodeQuery>(), Arg.Any<CancellationToken>())
            .Returns((codes, 1));

        ServiceResult<PaginatedResult<DiscountCodeDto>> result = await _sut.GetByRestaurantAsync(1, 1, new DiscountCodeQuery());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.TotalCount, Is.EqualTo(1));
        Assert.That(result.Value.Items, Has.Count.EqualTo(1));
        Assert.That(result.Value.Items[0].Code, Is.EqualTo("CODE1"));
    }

    [Test]
    public async Task UpdateAsync_WithValidData_ReturnsUpdatedCode()
    {
        DiscountCodeEntity code = CreateDiscountCode(restaurantId: 1);
        _discountCodeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(code);

        Restaurant restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        UpdateDiscountCodeRequest request = new UpdateDiscountCodeRequest
        {
            Description = "Nouvelle description",
            DiscountType = nameof(DiscountType.FixedAmount),
            DiscountValue = 15m,
            ValidFrom = new DateTime(2026, 7, 1),
            ValidUntil = new DateTime(2026, 9, 30),
            IsActive = true,
            PerUserLimit = 2
        };

        _discountCodeRepository.UpdateAsync(Arg.Any<DiscountCodeEntity>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<DiscountCodeEntity>(0));

        ServiceResult<DiscountCodeDto> result = await _sut.UpdateAsync(1, 1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Description, Is.EqualTo("Nouvelle description"));
        Assert.That(result.Value.DiscountType, Is.EqualTo(nameof(DiscountType.FixedAmount)));
        Assert.That(result.Value.DiscountValue, Is.EqualTo(15m));
    }

    [Test]
    public async Task UpdateAsync_WhenNotFound_ReturnsError()
    {
        _discountCodeRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((DiscountCodeEntity?)null);

        UpdateDiscountCodeRequest request = new UpdateDiscountCodeRequest
        {
            DiscountType = nameof(DiscountType.Percentage),
            DiscountValue = 10m,
            ValidFrom = new DateTime(2026, 6, 1),
            ValidUntil = new DateTime(2026, 8, 31)
        };

        ServiceResult<DiscountCodeDto> result = await _sut.UpdateAsync(99, 1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.DiscountCodeNotFound));
    }

    [Test]
    public async Task DeleteAsync_WhenOwner_ReturnsSuccess()
    {
        DiscountCodeEntity code = CreateDiscountCode(restaurantId: 1);
        _discountCodeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(code);

        Restaurant restaurant = CreateRestaurant(ownerId: 1);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        _discountCodeRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        ServiceResult result = await _sut.DeleteAsync(1, 1);

        Assert.That(result.IsSuccess, Is.True);
        await _discountCodeRepository.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteAsync_WhenNotOwner_ReturnsError()
    {
        DiscountCodeEntity code = CreateDiscountCode(restaurantId: 1);
        _discountCodeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(code);

        Restaurant restaurant = CreateRestaurant(ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        ServiceResult result = await _sut.DeleteAsync(1, 999);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
    }

    private static DiscountCodeEntity CreateDiscountCode(int restaurantId) => new()
    {
        Id = 1,
        RestaurantId = restaurantId,
        Code = "TESTCODE",
        DiscountType = DiscountType.Percentage,
        DiscountValue = 10m,
        ValidFrom = new DateTime(2026, 6, 1),
        ValidUntil = new DateTime(2026, 8, 31)
    };
}
