using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Admin;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class AdminRestaurantServiceTests
{
    private IRestaurantRepository _restaurantRepository = null!;
    private AdminRestaurantService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _restaurantRepository = Substitute.For<IRestaurantRepository>();
        _sut = new AdminRestaurantService(_restaurantRepository);
    }

    #region GetAllAsync

    [Test]
    public async Task GetAllAsync_ReturnsAllRestaurants()
    {
        var owner = CreateValidUser();
        owner.Id = 5;
        var restaurants = new List<Restaurant>
        {
            CreateRestaurant(id: 1, ownerId: 5),
            CreateRestaurant(id: 2, ownerId: 5)
        };
        restaurants[0].Owner = owner;
        restaurants[1].Owner = owner;

        _restaurantRepository.GetAllUnscopedAsync(Arg.Any<CancellationToken>()).Returns(restaurants);

        var result = await _sut.GetAllAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    #endregion

    #region GetByIdAsync

    [Test]
    public async Task GetByIdAsync_WhenExists_ReturnsRestaurant()
    {
        var owner = CreateValidUser();
        owner.Id = 5;
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        restaurant.Owner = owner;

        _restaurantRepository.GetByIdWithOwnerAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);

        var result = await _sut.GetByIdAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(1));
        Assert.That(result.Value.OwnerName, Is.EqualTo($"{owner.FirstName} {owner.LastName}"));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotExists_Returns404()
    {
        _restaurantRepository.GetByIdWithOwnerAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RestaurantNotFound));
    }

    #endregion

    #region UpdateAsync

    [Test]
    public async Task UpdateAsync_WhenExists_UpdatesAndReturns()
    {
        var owner = CreateValidUser();
        owner.Id = 5;
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        restaurant.Owner = owner;

        _restaurantRepository.GetByIdWithOwnerAsync(1, Arg.Any<CancellationToken>()).Returns(restaurant);
        _restaurantRepository.UpdateAsync(Arg.Any<Restaurant>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Restaurant>());

        var request = new AdminUpdateRestaurantRequest
        {
            Name = "Updated Name",
            AdressLine1 = "10 Rue Nouvelle",
            City = "Lyon",
            ZipCode = "69001",
            Country = "FR",
            IsActive = false
        };

        var result = await _sut.UpdateAsync(1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Name, Is.EqualTo("Updated Name"));
        Assert.That(result.Value.City, Is.EqualTo("Lyon"));
        Assert.That(result.Value.IsActive, Is.False);
    }

    [Test]
    public async Task UpdateAsync_WhenNotExists_Returns404()
    {
        _restaurantRepository.GetByIdWithOwnerAsync(99, Arg.Any<CancellationToken>()).Returns((Restaurant?)null);

        var request = new AdminUpdateRestaurantRequest
        {
            Name = "Name",
            AdressLine1 = "Addr",
            City = "City",
            ZipCode = "00000",
            Country = "FR"
        };

        var result = await _sut.UpdateAsync(99, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RestaurantNotFound));
    }

    #endregion

    #region DeleteAsync

    [Test]
    public async Task DeleteAsync_WhenExists_ReturnsSuccess()
    {
        _restaurantRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteAsync(1);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DeleteAsync_WhenNotExists_Returns404()
    {
        _restaurantRepository.DeleteAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.DeleteAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RestaurantNotFound));
    }

    #endregion
}
