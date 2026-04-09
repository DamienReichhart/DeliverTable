using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Rating;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class RatingServiceTests
{
    private IRatingRepository _ratingRepository = null!;
    private IOrderRepository _orderRepository = null!;
    private RatingService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _ratingRepository = Substitute.For<IRatingRepository>();
        _orderRepository = Substitute.For<IOrderRepository>();
        _sut = new RatingService(_ratingRepository, _orderRepository);
    }

    #region CreateAsync

    [Test]
    public async Task CreateAsync_WhenValid_ReturnsRatingDto()
    {
        var order = CreateOrder(id: 1, customerId: 10, restaurantId: 5, status: OrderStatus.Delivered);
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _ratingRepository.GetByOrderAndCustomerAsync(1, 10, Arg.Any<CancellationToken>()).Returns((RestaurantRating?)null);
        _ratingRepository.CreateAsync(Arg.Any<RestaurantRating>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var r = callInfo.Arg<RestaurantRating>();
                r.Id = 1;
                r.Restaurant = order.Restaurant;
                return r;
            });

        var request = new CreateRatingRequest { Rating = 5, Comment = "Excellent" };
        var result = await _sut.CreateAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Rating, Is.EqualTo(5));
        Assert.That(result.Value.Comment, Is.EqualTo("Excellent"));
        Assert.That(result.Value.OrderId, Is.EqualTo(1));
        Assert.That(result.Value.RestaurantId, Is.EqualTo(5));
    }

    [Test]
    public async Task CreateAsync_WhenOrderNotFound_Returns404()
    {
        _orderRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Order?)null);

        var request = new CreateRatingRequest { Rating = 5 };
        var result = await _sut.CreateAsync(99, 10, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.OrderNotFound));
    }

    [Test]
    public async Task CreateAsync_WhenOrderBelongsToOtherCustomer_Returns404()
    {
        var order = CreateOrder(id: 1, customerId: 99);
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var request = new CreateRatingRequest { Rating = 5 };
        var result = await _sut.CreateAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.OrderNotFound));
    }

    [Test]
    public async Task CreateAsync_WhenOrderNotDelivered_Returns400()
    {
        var order = CreateOrder(id: 1, customerId: 10, status: OrderStatus.Preparing);
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var request = new CreateRatingRequest { Rating = 5 };
        var result = await _sut.CreateAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.OrderNotDelivered));
    }

    [Test]
    public async Task CreateAsync_WhenRatingAlreadyExists_Returns409()
    {
        var order = CreateOrder(id: 1, customerId: 10, status: OrderStatus.Delivered);
        var existing = CreateRestaurantRating(orderId: 1, customerId: 10);
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _ratingRepository.GetByOrderAndCustomerAsync(1, 10, Arg.Any<CancellationToken>()).Returns(existing);

        var request = new CreateRatingRequest { Rating = 5 };
        var result = await _sut.CreateAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(409));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RatingAlreadyExists));
    }

    [Test]
    public async Task CreateAsync_WhenRatingTooLow_Returns400()
    {
        var order = CreateOrder(id: 1, customerId: 10, status: OrderStatus.Delivered);
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _ratingRepository.GetByOrderAndCustomerAsync(1, 10, Arg.Any<CancellationToken>()).Returns((RestaurantRating?)null);

        var request = new CreateRatingRequest { Rating = 0 };
        var result = await _sut.CreateAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RatingOutOfRange));
    }

    [Test]
    public async Task CreateAsync_WhenRatingTooHigh_Returns400()
    {
        var order = CreateOrder(id: 1, customerId: 10, status: OrderStatus.Delivered);
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _ratingRepository.GetByOrderAndCustomerAsync(1, 10, Arg.Any<CancellationToken>()).Returns((RestaurantRating?)null);

        var request = new CreateRatingRequest { Rating = 6 };
        var result = await _sut.CreateAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RatingOutOfRange));
    }

    [Test]
    public async Task CreateAsync_WithoutComment_DefaultsToEmpty()
    {
        var order = CreateOrder(id: 1, customerId: 10, restaurantId: 5, status: OrderStatus.Delivered);
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _ratingRepository.GetByOrderAndCustomerAsync(1, 10, Arg.Any<CancellationToken>()).Returns((RestaurantRating?)null);
        _ratingRepository.CreateAsync(Arg.Any<RestaurantRating>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var r = callInfo.Arg<RestaurantRating>();
                r.Id = 1;
                r.Restaurant = order.Restaurant;
                return r;
            });

        var request = new CreateRatingRequest { Rating = 3 };
        var result = await _sut.CreateAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Comment, Is.EqualTo(string.Empty));
    }

    #endregion

    #region GetByOrderAsync

    [Test]
    public async Task GetByOrderAsync_WhenExists_ReturnsRatingDto()
    {
        var rating = CreateRestaurantRating(id: 1, orderId: 1, restaurantId: 5, customerId: 10);
        _ratingRepository.GetByOrderAndCustomerAsync(1, 10, Arg.Any<CancellationToken>()).Returns(rating);

        var result = await _sut.GetByOrderAsync(1, 10);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(1));
        Assert.That(result.Value.OrderId, Is.EqualTo(1));
    }

    [Test]
    public async Task GetByOrderAsync_WhenNotFound_Returns404()
    {
        _ratingRepository.GetByOrderAndCustomerAsync(1, 10, Arg.Any<CancellationToken>()).Returns((RestaurantRating?)null);

        var result = await _sut.GetByOrderAsync(1, 10);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RatingNotFound));
    }

    #endregion

    #region UpdateAsync

    [Test]
    public async Task UpdateAsync_WhenValid_ReturnsUpdatedRatingDto()
    {
        var existing = CreateRestaurantRating(id: 1, orderId: 1, restaurantId: 5, customerId: 10, rating: 3);
        _ratingRepository.GetByOrderAndCustomerAsync(1, 10, Arg.Any<CancellationToken>()).Returns(existing);
        _ratingRepository.UpdateAsync(Arg.Any<RestaurantRating>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<RestaurantRating>());

        var request = new UpdateRatingRequest { Rating = 5, Comment = "Finalement excellent" };
        var result = await _sut.UpdateAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Rating, Is.EqualTo(5));
        Assert.That(result.Value.Comment, Is.EqualTo("Finalement excellent"));
    }

    [Test]
    public async Task UpdateAsync_WhenRatingNotFound_Returns404()
    {
        _ratingRepository.GetByOrderAndCustomerAsync(1, 10, Arg.Any<CancellationToken>()).Returns((RestaurantRating?)null);

        var request = new UpdateRatingRequest { Rating = 5 };
        var result = await _sut.UpdateAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RatingNotFound));
    }

    [Test]
    public async Task UpdateAsync_WhenRatingOutOfRange_Returns400()
    {
        var existing = CreateRestaurantRating(id: 1, orderId: 1, customerId: 10);
        _ratingRepository.GetByOrderAndCustomerAsync(1, 10, Arg.Any<CancellationToken>()).Returns(existing);

        var request = new UpdateRatingRequest { Rating = 0 };
        var result = await _sut.UpdateAsync(1, 10, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RatingOutOfRange));
    }

    #endregion

    #region DeleteAsync

    [Test]
    public async Task DeleteAsync_WhenExists_ReturnsSuccess()
    {
        var existing = CreateRestaurantRating(id: 1, orderId: 1, customerId: 10);
        _ratingRepository.GetByOrderAndCustomerAsync(1, 10, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.DeleteAsync(1, 10);

        Assert.That(result.IsSuccess, Is.True);
        await _ratingRepository.Received(1).DeleteAsync(existing, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteAsync_WhenNotFound_Returns404()
    {
        _ratingRepository.GetByOrderAndCustomerAsync(1, 10, Arg.Any<CancellationToken>()).Returns((RestaurantRating?)null);

        var result = await _sut.DeleteAsync(1, 10);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.RatingNotFound));
    }

    #endregion
}
