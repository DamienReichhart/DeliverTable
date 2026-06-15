using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;
using NSubstitute;
using static DeliverTableTests.Server.Factories.ServerEntityFactory;
using DeliverTableServer.Common;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class AdminOrderServiceTests
{
    private IOrderRepository _orderRepository = null!;
    private AdminOrderService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _orderRepository = Substitute.For<IOrderRepository>();
        _sut = new AdminOrderService(_orderRepository);
    }

    #region GetAllAsync

    [Test]
    public async Task GetAllAsync_ReturnsAllOrders()
    {
        User customer = CreateValidUser();
        customer.Id = 1;
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        List<Order> orders = new List<Order>
        {
            new()
            {
                Id = 1, CustomerId = 1, Customer = customer,
                RestaurantId = 1, Restaurant = restaurant,
                Status = OrderStatus.Pending, Items = [], Payments = []
            },
            new()
            {
                Id = 2, CustomerId = 1, Customer = customer,
                RestaurantId = 1, Restaurant = restaurant,
                Status = OrderStatus.Confirmed, Items = [], Payments = []
            }
        };

        _orderRepository.GetAllUnscopedAsync(Arg.Any<CancellationToken>()).Returns(orders);

        ServiceResult<List<AdminOrderResponse>> result = await _sut.GetAllAsync();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    #endregion

    #region GetByIdAsync

    [Test]
    public async Task GetByIdAsync_WhenExists_ReturnsOrder()
    {
        User customer = CreateValidUser();
        customer.Id = 1;
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        Order order = new Order
        {
            Id = 1,
            CustomerId = 1,
            Customer = customer,
            RestaurantId = 1,
            Restaurant = restaurant,
            Status = OrderStatus.Pending,
            TotalAmount = 42.50m,
            Items =
            [
                new OrderItem { Id = 1, DishName = "Plat A", Quantity = 2, UnitPrice = 10m }
            ],
            Payments =
            [
                new Payment { Id = 1, Provider = "Stripe", Amount = 42.50m, Currency = "EUR" }
            ]
        };

        _orderRepository.GetByIdWithFullDetailsAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        ServiceResult<AdminOrderResponse> result = await _sut.GetByIdAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Id, Is.EqualTo(1));
        Assert.That(result.Value.TotalAmount, Is.EqualTo(42.50m));
        Assert.That(result.Value.CustomerName, Is.EqualTo($"{customer.FirstName} {customer.LastName}"));
        Assert.That(result.Value.RestaurantName, Is.EqualTo(restaurant.Name));
        Assert.That(result.Value.Items, Has.Count.EqualTo(1));
        Assert.That(result.Value.Payments, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotExists_Returns404()
    {
        _orderRepository.GetByIdWithFullDetailsAsync(99, Arg.Any<CancellationToken>()).Returns((Order?)null);

        ServiceResult<AdminOrderResponse> result = await _sut.GetByIdAsync(99);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.OrderNotFound));
    }

    #endregion

    #region UpdateStatusAsync

    [Test]
    public async Task UpdateStatusAsync_WhenValidStatus_UpdatesAndReturns()
    {
        User customer = CreateValidUser();
        customer.Id = 1;
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        Order order = new Order
        {
            Id = 1,
            CustomerId = 1,
            Customer = customer,
            RestaurantId = 1,
            Restaurant = restaurant,
            Status = OrderStatus.Pending,
            Items = [],
            Payments = []
        };

        _orderRepository.GetByIdWithFullDetailsAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.UpdateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Order>());

        AdminUpdateOrderStatusRequest request = new AdminUpdateOrderStatusRequest { Status = nameof(OrderStatus.Confirmed) };

        ServiceResult<AdminOrderResponse> result = await _sut.UpdateStatusAsync(1, request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Status, Is.EqualTo(nameof(OrderStatus.Confirmed)));
    }

    [Test]
    public async Task UpdateStatusAsync_WhenOrderNotFound_Returns404()
    {
        _orderRepository.GetByIdWithFullDetailsAsync(99, Arg.Any<CancellationToken>()).Returns((Order?)null);

        AdminUpdateOrderStatusRequest request = new AdminUpdateOrderStatusRequest { Status = nameof(OrderStatus.Confirmed) };

        ServiceResult<AdminOrderResponse> result = await _sut.UpdateStatusAsync(99, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(404));
        Assert.That(result.Error.Message, Is.EqualTo(ErrorMessages.OrderNotFound));
    }

    [Test]
    public async Task UpdateStatusAsync_WhenInvalidStatus_Returns400()
    {
        User customer = CreateValidUser();
        customer.Id = 1;
        Restaurant restaurant = CreateRestaurant(id: 1, ownerId: 5);
        Order order = new Order
        {
            Id = 1,
            CustomerId = 1,
            Customer = customer,
            RestaurantId = 1,
            Restaurant = restaurant,
            Status = OrderStatus.Pending,
            Items = [],
            Payments = []
        };

        _orderRepository.GetByIdWithFullDetailsAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        AdminUpdateOrderStatusRequest request = new AdminUpdateOrderStatusRequest { Status = "InvalidStatus" };

        ServiceResult<AdminOrderResponse> result = await _sut.UpdateStatusAsync(1, request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.StatusCode, Is.EqualTo(400));
    }

    #endregion
}
