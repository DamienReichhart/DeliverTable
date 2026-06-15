using System.Reflection;
using DeliverTableServer.Common;
using DeliverTableServer.Features.Admin;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Dtos.Payment;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminOrderControllerTests
{
    private IAdminOrderService _adminOrderService = null!;
    private IPaymentService _paymentService = null!;
    private AdminOrderController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _adminOrderService = Substitute.For<IAdminOrderService>();
        _paymentService = Substitute.For<IPaymentService>();
        _sut = new AdminOrderController(_adminOrderService, _paymentService);
    }

    #region GetAll

    [Test]
    public async Task GetAll_ReturnsOk()
    {
        List<AdminOrderResponse> orders = new List<AdminOrderResponse>
        {
            new() { Id = 1, Status = nameof(OrderStatus.Pending) },
            new() { Id = 2, Status = nameof(OrderStatus.Confirmed) }
        };
        _adminOrderService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminOrderResponse>>.Success(orders));

        IActionResult result = await _sut.GetAll(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAll_WhenError_ReturnsError()
    {
        _adminOrderService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminOrderResponse>>.Failure(new ServiceError("Erreur", 500)));

        IActionResult result = await _sut.GetAll(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        ObjectResult obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region GetById

    [Test]
    public async Task GetById_WhenExists_ReturnsOk()
    {
        AdminOrderResponse order = new AdminOrderResponse { Id = 1, Status = nameof(OrderStatus.Pending) };
        _adminOrderService.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderResponse>.Success(order));

        IActionResult result = await _sut.GetById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_WhenNotFound_Returns404()
    {
        _adminOrderService.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderResponse>.Failure(new ServiceError("Commande introuvable", 404)));

        IActionResult result = await _sut.GetById(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        ObjectResult obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region UpdateStatus

    [Test]
    public async Task UpdateStatus_WhenSuccess_ReturnsOk()
    {
        AdminUpdateOrderStatusRequest request = new AdminUpdateOrderStatusRequest { Status = nameof(OrderStatus.Confirmed) };
        AdminOrderResponse response = new AdminOrderResponse { Id = 1, Status = nameof(OrderStatus.Confirmed) };
        _adminOrderService.UpdateStatusAsync(1, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderResponse>.Success(response));

        IActionResult result = await _sut.UpdateStatus(1, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task UpdateStatus_WhenNotFound_Returns404()
    {
        AdminUpdateOrderStatusRequest request = new AdminUpdateOrderStatusRequest { Status = nameof(OrderStatus.Confirmed) };
        _adminOrderService.UpdateStatusAsync(99, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderResponse>.Failure(new ServiceError("Commande introuvable", 404)));

        IActionResult result = await _sut.UpdateStatus(99, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        ObjectResult obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task UpdateStatus_WhenInvalidStatus_Returns400()
    {
        AdminUpdateOrderStatusRequest request = new AdminUpdateOrderStatusRequest { Status = "InvalidStatus" };
        _adminOrderService.UpdateStatusAsync(1, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderResponse>.Failure(new ServiceError("Statut invalide", 400)));

        IActionResult result = await _sut.UpdateStatus(1, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        ObjectResult obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(400));
    }

    #endregion

    #region RefundOrder

    [Test]
    public async Task RefundOrder_HappyPath_ReturnsOkWithRefundDto()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "99", nameof(UserRole.Administrator));
        RefundDto refundDto = new RefundDto(1, 20m, "EUR", "mistake", DateTime.UtcNow);
        _paymentService
            .RefundAsync(42, 20m, "mistake", 99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefundDto>.Success(refundDto));

        IActionResult result = await _sut.RefundOrder(42, new AdminRefundRequest { Amount = 20m, Reason = "mistake" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task RefundOrder_WhenUnauthenticated_ReturnsUnauthorized()
    {
        AuthenticationTestHelper.SetupUnauthenticatedUser(_sut);

        IActionResult result = await _sut.RefundOrder(42, new AdminRefundRequest { Amount = 20m, Reason = "mistake" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task RefundOrder_WhenServiceFails_ReturnsError()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "99", nameof(UserRole.Administrator));
        _paymentService
            .RefundAsync(42, 20m, "mistake", 99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefundDto>.Failure(new ServiceError("Remboursement impossible", 400)));

        IActionResult result = await _sut.RefundOrder(42, new AdminRefundRequest { Amount = 20m, Reason = "mistake" }, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        ObjectResult obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public void RefundOrder_HasAdministratorAuthorizeAttribute()
    {
        MethodInfo method = typeof(AdminOrderController).GetMethod(nameof(AdminOrderController.RefundOrder))!;
        IEnumerable<AuthorizeAttribute> methodAttrs = method
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>();
        IEnumerable<AuthorizeAttribute> controllerAttrs = typeof(AdminOrderController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>();

        Assert.That(
            methodAttrs.Concat(controllerAttrs)
                .Any(a => a.Roles?.Contains(nameof(UserRole.Administrator)) == true),
            Is.True);
    }

    #endregion
}
