using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;
using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;

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
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        User customer = CreateValidUser();
        customer.Id = 1;
        List<RestaurantRating> ratings = new List<RestaurantRating>
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

        ServiceResult<List<AdminRestaurantRatingResponse>> result = await _sut.GetRestaurantRatingsAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetRestaurantRatingsAsync_MapsFieldsCorrectly()
    {
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        User customer = CreateValidUser();
        customer.Id = 1;
        List<RestaurantRating> ratings = new List<RestaurantRating>
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

        ServiceResult<List<AdminRestaurantRatingResponse>> result = await _sut.GetRestaurantRatingsAsync();

        Assert.That(result.IsSuccess, Is.True);
        AdminRestaurantRatingResponse dto = result.Value![0];
        Assert.That(dto.Id, Is.EqualTo(1));
        Assert.That(dto.Rating, Is.EqualTo(5));
        Assert.That(dto.Comment, Is.EqualTo("Excellent"));
        Assert.That(dto.RestaurantName, Is.EqualTo(restaurant.Name));
        Assert.That(dto.CustomerName, Is.EqualTo($"{customer.FirstName} {customer.LastName}"));
        Assert.That(dto.OrderId, Is.EqualTo(10));
    }

    #endregion

    #region DeleteAsync

    [Test]
    public async Task DeleteAsync_WhenRatingExists_ReturnsSuccess()
    {
        _ratingRepository.DeleteRestaurantRatingAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        ServiceResult result = await _sut.DeleteAsync(1);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DeleteAsync_WhenNotFound_Returns404()
    {
        _ratingRepository.DeleteRestaurantRatingAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        ServiceResult result = await _sut.DeleteAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RatingNotFound));
    }

    #endregion
}
