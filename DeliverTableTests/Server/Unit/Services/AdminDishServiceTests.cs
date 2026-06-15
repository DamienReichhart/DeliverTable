using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Admin;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;
using DeliverTableServer.Common;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class AdminDishServiceTests
{
    private IDishRepository _dishRepository = null!;
    private IRestaurantRepository _restaurantRepository = null!;
    private AdminDishService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dishRepository = Substitute.For<IDishRepository>();
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _sut = new AdminDishService(_dishRepository, _restaurantRepository);
    }

    #region GetAllAsync

    [Test]
    public async Task GetAllAsync_ReturnsAllDishes()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        List<Dish> dishes = new List<Dish>
        {
            new() { Id = 1, Name = "Plat A", RestaurantId = 1, Restaurant = restaurant },
            new() { Id = 2, Name = "Plat B", RestaurantId = 1, Restaurant = restaurant }
        };

        _dishRepository.GetAllUnscopedAsync(Arg.Any<CancellationToken>()).Returns(dishes);

        ServiceResult<List<AdminDishResponse>> result = await _sut.GetAllAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    #endregion

    #region GetByIdAsync

    [Test]
    public async Task GetByIdAsync_WhenExists_ReturnsDish()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        Dish dish = new Dish { Id = 1, Name = "Plat A", RestaurantId = 1, Restaurant = restaurant };

        _dishRepository.GetByIdWithRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(dish);

        ServiceResult<AdminDishResponse> result = await _sut.GetByIdAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(1));
        Assert.That(result.Value.RestaurantName, Is.EqualTo(restaurant.Name));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotExists_Returns404()
    {
        _dishRepository.GetByIdWithRestaurantAsync(99, Arg.Any<CancellationToken>()).Returns((Dish?)null);

        ServiceResult<AdminDishResponse> result = await _sut.GetByIdAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.DishNotFound));
    }

    #endregion

    #region CreateAsync

    [Test]
    public async Task CreateAsync_WhenRestaurantExists_CreatesDish()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        _restaurantRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _dishRepository.CreateAsync(Arg.Any<Dish>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                Dish d = callInfo.Arg<Dish>();
                d.Id = 10;
                d.Restaurant = restaurant;
                return d;
            });

        AdminCreateDishRequest request = new AdminCreateDishRequest
        {
            Name = "Nouveau Plat",
            Description = "Description",
            BasePrice = 12.50m,
            RestaurantId = 1,
            IsVegetarian = true,
            IsActive = true
        };

        ServiceResult<AdminDishResponse> result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Name, Is.EqualTo("Nouveau Plat"));
        Assert.That(result.Value.BasePrice, Is.EqualTo(12.50m));
        Assert.That(result.Value.IsVegetarian, Is.True);
    }

    [Test]
    public async Task CreateAsync_WhenRestaurantNotExists_Returns404()
    {
        _restaurantRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        AdminCreateDishRequest request = new AdminCreateDishRequest
        {
            Name = "Plat",
            BasePrice = 10m,
            RestaurantId = 99
        };

        ServiceResult<AdminDishResponse> result = await _sut.CreateAsync(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RestaurantNotFound));
    }

    #endregion

    #region UpdateAsync

    [Test]
    public async Task UpdateAsync_WhenExists_UpdatesAndReturns()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        Dish dish = new Dish { Id = 1, Name = "Ancien", RestaurantId = 1, Restaurant = restaurant };

        _dishRepository.GetByIdWithRestaurantAsync(1, Arg.Any<CancellationToken>()).Returns(dish);
        _dishRepository.UpdateAsync(Arg.Any<Dish>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Dish>());

        AdminUpdateDishRequest request = new AdminUpdateDishRequest
        {
            Name = "Nouveau Nom",
            BasePrice = 15.00m,
            IsVegan = true,
            IsActive = false
        };

        ServiceResult<AdminDishResponse> result = await _sut.UpdateAsync(1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Name, Is.EqualTo("Nouveau Nom"));
        Assert.That(result.Value.BasePrice, Is.EqualTo(15.00m));
        Assert.That(result.Value.IsVegan, Is.True);
        Assert.That(result.Value.IsActive, Is.False);
    }

    [Test]
    public async Task UpdateAsync_WhenNotExists_Returns404()
    {
        _dishRepository.GetByIdWithRestaurantAsync(99, Arg.Any<CancellationToken>()).Returns((Dish?)null);

        AdminUpdateDishRequest request = new AdminUpdateDishRequest
        {
            Name = "Name",
            BasePrice = 10m
        };

        ServiceResult<AdminDishResponse> result = await _sut.UpdateAsync(99, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.DishNotFound));
    }

    #endregion

    #region DeleteAsync

    [Test]
    public async Task DeleteAsync_WhenExists_ReturnsSuccess()
    {
        _dishRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        ServiceResult result = await _sut.DeleteAsync(1);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DeleteAsync_WhenNotExists_Returns404()
    {
        _dishRepository.DeleteAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        ServiceResult result = await _sut.DeleteAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.DishNotFound));
    }

    #endregion
}
