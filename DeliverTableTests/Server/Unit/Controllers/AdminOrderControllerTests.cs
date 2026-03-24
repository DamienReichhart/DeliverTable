using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminOrderControllerTests
{
    private IAdminOrderService _adminOrderService = null!;
    private AdminOrderController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _adminOrderService = Substitute.For<IAdminOrderService>();
        _sut = new AdminOrderController(_adminOrderService);
    }

    #region GetAll

    [Test]
    public async Task GetAll_ReturnsOk()
    {
        var orders = new List<AdminOrderResponse>
        {
            new() { Id = 1, Status = nameof(OrderStatus.Pending) },
            new() { Id = 2, Status = nameof(OrderStatus.Confirmed) }
        };
        _adminOrderService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminOrderResponse>>.Success(orders));

        var result = await _sut.GetAll(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAll_WhenError_ReturnsError()
    {
        _adminOrderService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminOrderResponse>>.Failure(new ServiceError("Erreur", 500)));

        var result = await _sut.GetAll(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region GetById

    [Test]
    public async Task GetById_WhenExists_ReturnsOk()
    {
        var order = new AdminOrderResponse { Id = 1, Status = nameof(OrderStatus.Pending) };
        _adminOrderService.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderResponse>.Success(order));

        var result = await _sut.GetById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_WhenNotFound_Returns404()
    {
        _adminOrderService.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderResponse>.Failure(new ServiceError("Commande introuvable", 404)));

        var result = await _sut.GetById(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region UpdateStatus

    [Test]
    public async Task UpdateStatus_WhenSuccess_ReturnsOk()
    {
        var request = new AdminUpdateOrderStatusRequest { Status = nameof(OrderStatus.Confirmed) };
        var response = new AdminOrderResponse { Id = 1, Status = nameof(OrderStatus.Confirmed) };
        _adminOrderService.UpdateStatusAsync(1, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderResponse>.Success(response));

        var result = await _sut.UpdateStatus(1, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task UpdateStatus_WhenNotFound_Returns404()
    {
        var request = new AdminUpdateOrderStatusRequest { Status = nameof(OrderStatus.Confirmed) };
        _adminOrderService.UpdateStatusAsync(99, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderResponse>.Failure(new ServiceError("Commande introuvable", 404)));

        var result = await _sut.UpdateStatus(99, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task UpdateStatus_WhenInvalidStatus_Returns400()
    {
        var request = new AdminUpdateOrderStatusRequest { Status = "InvalidStatus" };
        _adminOrderService.UpdateStatusAsync(1, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderResponse>.Failure(new ServiceError("Statut invalide", 400)));

        var result = await _sut.UpdateStatus(1, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(400));
    }

    #endregion
}
