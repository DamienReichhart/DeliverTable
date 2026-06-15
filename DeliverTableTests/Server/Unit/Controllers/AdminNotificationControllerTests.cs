using DeliverTableServer.Common;
using DeliverTableServer.Features.Admin;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminNotificationControllerTests
{
    private IAdminNotificationService _adminNotificationService = null!;
    private AdminNotificationController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _adminNotificationService = Substitute.For<IAdminNotificationService>();
        _sut = new AdminNotificationController(_adminNotificationService);
    }

    #region GetAll

    [Test]
    public async Task GetAll_ReturnsOk()
    {
        List<AdminNotificationResponse> notifications = new List<AdminNotificationResponse>
        {
            new() { Id = 1, Type = "OrderStatus", Payload = "Commande confirmée" },
            new() { Id = 2, Type = "System", Payload = "Maintenance prévue" }
        };
        _adminNotificationService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminNotificationResponse>>.Success(notifications));

        IActionResult result = await _sut.GetAll(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAll_WhenError_ReturnsError()
    {
        _adminNotificationService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminNotificationResponse>>.Failure(new ServiceError("Erreur", 500)));

        IActionResult result = await _sut.GetAll(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        ObjectResult obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region Delete

    [Test]
    public async Task Delete_WhenSuccess_ReturnsNoContent()
    {
        _adminNotificationService.DeleteAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        IActionResult result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_WhenNotFound_Returns404()
    {
        _adminNotificationService.DeleteAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure(new ServiceError("Notification introuvable", 404)));

        IActionResult result = await _sut.Delete(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        ObjectResult obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion
}
