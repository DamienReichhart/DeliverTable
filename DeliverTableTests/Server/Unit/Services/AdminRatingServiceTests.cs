using DeliverTableServer.Constants;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class AdminRatingServiceTests
{
    private IRatingRepository _ratingRepository = null!;
    private AdminRatingService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _ratingRepository = Substitute.For<IRatingRepository>();
        _sut = new AdminRatingService(_ratingRepository);
    }

    #region GetRestaurantRatingsAsync

    [Test]
    public async Task GetRestaurantRatingsAsync_ReturnsAllRatings()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var customer = CreateValidUser();
        customer.Id = 1;
        var ratings = new List<RestaurantRating>
        {
            new()
            {
                Id = 1, Rating = 5, Comment = "Excellent",
                RestaurantId = 1, Restaurant = restaurant,
                CustomerUserId = 1, CustomerUser = customer,
                OrderId = 10
            },
            new()
            {
                Id = 2, Rating = 3, Comment = "Moyen",
                RestaurantId = 1, Restaurant = restaurant,
                CustomerUserId = 1, CustomerUser = customer,
                OrderId = 11
            }
        };

        _ratingRepository.GetAllRestaurantRatingsAsync(Arg.Any<CancellationToken>()).Returns(ratings);

        var result = await _sut.GetRestaurantRatingsAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetRestaurantRatingsAsync_MapsFieldsCorrectly()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var customer = CreateValidUser();
        customer.Id = 1;
        var ratings = new List<RestaurantRating>
        {
            new()
            {
                Id = 1, Rating = 5, Comment = "Excellent",
                RestaurantId = 1, Restaurant = restaurant,
                CustomerUserId = 1, CustomerUser = customer,
                OrderId = 10
            }
        };

        _ratingRepository.GetAllRestaurantRatingsAsync(Arg.Any<CancellationToken>()).Returns(ratings);

        var result = await _sut.GetRestaurantRatingsAsync();

        Assert.That(result.IsSuccess, Is.True);
        var dto = result.Value![0];
        Assert.That(dto.Id, Is.EqualTo(1));
        Assert.That(dto.Rating, Is.EqualTo(5));
        Assert.That(dto.Comment, Is.EqualTo("Excellent"));
        Assert.That(dto.RestaurantName, Is.EqualTo(restaurant.Name));
        Assert.That(dto.CustomerName, Is.EqualTo($"{customer.FirstName} {customer.LastName}"));
        Assert.That(dto.OrderId, Is.EqualTo(10));
    }

    #endregion

    #region GetCustomerRatingsAsync

    [Test]
    public async Task GetCustomerRatingsAsync_ReturnsAllRatings()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var ratedCustomer = CreateValidUser();
        ratedCustomer.Id = 2;
        var rater = CreateValidUser();
        rater.Id = 3;
        var ratings = new List<CustomerRating>
        {
            new()
            {
                Id = 1, Rating = 4, Comment = "Bon client",
                RestaurantId = 1, Restaurant = restaurant,
                RatedCustomerUserId = 2, RatedCustomerUser = ratedCustomer,
                RestaurantUserId = 3, RestaurantUser = rater,
                OrderId = 10
            }
        };

        _ratingRepository.GetAllCustomerRatingsAsync(Arg.Any<CancellationToken>()).Returns(ratings);

        var result = await _sut.GetCustomerRatingsAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetCustomerRatingsAsync_MapsFieldsCorrectly()
    {
        var restaurant = CreateRestaurant(id: 1, ownerId: 5);
        var ratedCustomer = CreateValidUser();
        ratedCustomer.Id = 2;
        var rater = CreateValidUser();
        rater.Id = 3;
        var ratings = new List<CustomerRating>
        {
            new()
            {
                Id = 1, Rating = 4, Comment = "Bon client",
                RestaurantId = 1, Restaurant = restaurant,
                RatedCustomerUserId = 2, RatedCustomerUser = ratedCustomer,
                RestaurantUserId = 3, RestaurantUser = rater,
                OrderId = 10
            }
        };

        _ratingRepository.GetAllCustomerRatingsAsync(Arg.Any<CancellationToken>()).Returns(ratings);

        var result = await _sut.GetCustomerRatingsAsync();

        Assert.That(result.IsSuccess, Is.True);
        var dto = result.Value![0];
        Assert.That(dto.Id, Is.EqualTo(1));
        Assert.That(dto.Rating, Is.EqualTo(4));
        Assert.That(dto.Comment, Is.EqualTo("Bon client"));
        Assert.That(dto.RestaurantName, Is.EqualTo(restaurant.Name));
        Assert.That(dto.RatedCustomerName, Is.EqualTo($"{ratedCustomer.FirstName} {ratedCustomer.LastName}"));
        Assert.That(dto.RaterName, Is.EqualTo($"{rater.FirstName} {rater.LastName}"));
        Assert.That(dto.OrderId, Is.EqualTo(10));
    }

    #endregion

    #region DeleteAsync

    [Test]
    public async Task DeleteAsync_WhenRestaurantRatingExists_ReturnsSuccess()
    {
        _ratingRepository.DeleteRestaurantRatingAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteAsync(1);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DeleteAsync_WhenCustomerRatingExists_ReturnsSuccess()
    {
        _ratingRepository.DeleteRestaurantRatingAsync(1, Arg.Any<CancellationToken>()).Returns(false);
        _ratingRepository.DeleteCustomerRatingAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteAsync(1);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DeleteAsync_WhenNotFound_Returns404()
    {
        _ratingRepository.DeleteRestaurantRatingAsync(99, Arg.Any<CancellationToken>()).Returns(false);
        _ratingRepository.DeleteCustomerRatingAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.DeleteAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RatingNotFound));
    }

    #endregion
}
