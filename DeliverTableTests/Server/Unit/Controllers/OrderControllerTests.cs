using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Dtos.Payment;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Helpers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class OrderControllerTests
{
    private IOrderService _orderService = null!;
    private OrderController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _orderService = Substitute.For<IOrderService>();
        _sut = new OrderController(_orderService);
    }

    // ─── CreateOrder ──────────────────────────────────────────────────────

    [Test]
    public async Task CreateOrder_HappyPath_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "10", nameof(UserRole.Customer));
        var request = new CreateOrderRequest
        {
            RestaurantId = 1,
            OrderType = nameof(OrderType.Delivery),
            DeliveryAddress = "123 Rue Test",
            DiscountCodes = [],
            LoyaltyPointsToRedeem = 0
        };
        var response = new CreateOrderResponse(42, "pi_secret", "pk_test", 70m, "EUR");
        _orderService.CreateFromCartAsync(10, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CreateOrderResponse>.Success(response));

        var result = await _sut.CreateOrder(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.InstanceOf<CreateOrderResponse>());
        var value = (CreateOrderResponse)ok.Value!;
        Assert.That(value.OrderId, Is.EqualTo(42));
        Assert.That(value.ClientSecret, Is.EqualTo("pi_secret"));
    }

    [Test]
    public async Task CreateOrder_ServiceError_ReturnsErrorResult()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "10", nameof(UserRole.Customer));
        var request = new CreateOrderRequest
        {
            RestaurantId = 1,
            OrderType = nameof(OrderType.Delivery),
            DeliveryAddress = "123 Rue Test",
            DiscountCodes = [],
            LoyaltyPointsToRedeem = 0
        };
        _orderService.CreateFromCartAsync(10, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CreateOrderResponse>.Failure(new ServiceError("Panier vide", 400)));

        var result = await _sut.CreateOrder(request, CancellationToken.None);

        Assert.That(result, Is.Not.InstanceOf<OkObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task CreateOrder_UnauthenticatedUser_ReturnsUnauthorized()
    {
        AuthenticationTestHelper.SetupUnauthenticatedUser(_sut);
        var request = new CreateOrderRequest
        {
            RestaurantId = 1,
            OrderType = nameof(OrderType.Delivery),
            DeliveryAddress = "123 Rue Test",
            DiscountCodes = [],
            LoyaltyPointsToRedeem = 0
        };

        var result = await _sut.CreateOrder(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    // ─── GetById ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetById_WhenExists_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "10", nameof(UserRole.Customer));
        var dto = new OrderDto { Id = 1, Status = nameof(OrderStatus.AwaitingPayment) };
        _orderService.GetByIdAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<OrderDto>.Success(dto));

        var result = await _sut.GetById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_WhenNotFound_ReturnsError()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "10", nameof(UserRole.Customer));
        _orderService.GetByIdAsync(99, 10, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<OrderDto>.Failure(new ServiceError("Commande introuvable", 404)));

        var result = await _sut.GetById(99, CancellationToken.None);

        Assert.That(result, Is.Not.InstanceOf<OkObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    // ─── GetMyOrders ──────────────────────────────────────────────────────

    [Test]
    public async Task GetMyOrders_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "10", nameof(UserRole.Customer));
        var paginated = new PaginatedResult<OrderDto>
        {
            Items = [new OrderDto { Id = 1, Status = nameof(OrderStatus.Pending) }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };
        _orderService.GetCustomerOrdersAsync(10, Arg.Any<OrderQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<OrderDto>>.Success(paginated));

        var result = await _sut.GetMyOrders(new OrderQuery(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    // ─── CancelOrder ──────────────────────────────────────────────────────

    [Test]
    public async Task CancelOrder_HappyPath_ReturnsOk()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "10", nameof(UserRole.Customer));
        var dto = new OrderDto { Id = 1, Status = nameof(OrderStatus.Cancelled) };
        _orderService.CancelOrderAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<OrderDto>.Success(dto));

        var result = await _sut.CancelOrder(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task CancelOrder_ServiceError_ReturnsError()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "10", nameof(UserRole.Customer));
        _orderService.CancelOrderAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<OrderDto>.Failure(new ServiceError("Annulation impossible", 400)));

        var result = await _sut.CancelOrder(1, CancellationToken.None);

        Assert.That(result, Is.Not.InstanceOf<OkObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(400));
    }
}
