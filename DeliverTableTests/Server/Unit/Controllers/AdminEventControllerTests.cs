using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminEventControllerTests
{
    private IAdminEventService _adminEventService = null!;
    private AdminEventController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _adminEventService = Substitute.For<IAdminEventService>();
        _sut = new AdminEventController(_adminEventService);
    }

    #region GetAll

    [Test]
    public async Task GetAll_ReturnsOk()
    {
        var events = new List<AdminEventResponse>
        {
            new() { Id = 1, Name = "Événement A" },
            new() { Id = 2, Name = "Événement B" }
        };
        _adminEventService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminEventResponse>>.Success(events));

        var result = await _sut.GetAll(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAll_WhenError_ReturnsError()
    {
        _adminEventService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminEventResponse>>.Failure(new ServiceError("Erreur", 500)));

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
        var evt = new AdminEventResponse { Id = 1, Name = "Événement A" };
        _adminEventService.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminEventResponse>.Success(evt));

        var result = await _sut.GetById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_WhenNotFound_Returns404()
    {
        _adminEventService.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminEventResponse>.Failure(new ServiceError("Événement introuvable", 404)));

        var result = await _sut.GetById(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region Create

    [Test]
    public async Task Create_WhenSuccess_ReturnsCreated()
    {
        var request = new AdminCreateEventRequest
        {
            Name = "Nouvel Événement",
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            CreatedByUserId = 5,
            IsActive = true
        };
        var response = new AdminEventResponse { Id = 10, Name = "Nouvel Événement" };
        _adminEventService.CreateAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminEventResponse>.Success(response));

        var result = await _sut.Create(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
        var created = (CreatedAtActionResult)result;
        Assert.That(created.ActionName, Is.EqualTo(nameof(AdminEventController.GetById)));
    }

    [Test]
    public async Task Create_WhenError_ReturnsError()
    {
        var request = new AdminCreateEventRequest
        {
            Name = "Événement",
            StartsAt = DateTime.UtcNow.AddDays(2),
            EndsAt = DateTime.UtcNow.AddDays(1),
            CreatedByUserId = 5
        };
        _adminEventService.CreateAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminEventResponse>.Failure(new ServiceError("Dates invalides", 400)));

        var result = await _sut.Create(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(400));
    }

    #endregion

    #region Update

    [Test]
    public async Task Update_WhenSuccess_ReturnsOk()
    {
        var request = new AdminUpdateEventRequest
        {
            Name = "Mis à jour",
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            IsActive = true
        };
        var response = new AdminEventResponse { Id = 1, Name = "Mis à jour" };
        _adminEventService.UpdateAsync(1, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminEventResponse>.Success(response));

        var result = await _sut.Update(1, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Update_WhenNotFound_Returns404()
    {
        var request = new AdminUpdateEventRequest
        {
            Name = "Name",
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(1)
        };
        _adminEventService.UpdateAsync(99, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminEventResponse>.Failure(new ServiceError("Événement introuvable", 404)));

        var result = await _sut.Update(99, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region Delete

    [Test]
    public async Task Delete_WhenSuccess_ReturnsNoContent()
    {
        _adminEventService.DeleteAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_WhenNotFound_Returns404()
    {
        _adminEventService.DeleteAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure(new ServiceError("Événement introuvable", 404)));

        var result = await _sut.Delete(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion
}
