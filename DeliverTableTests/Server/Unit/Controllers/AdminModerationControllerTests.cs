using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Helpers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminModerationControllerTests
{
    private IAdminModerationService _adminModerationService = null!;
    private AdminModerationController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _adminModerationService = Substitute.For<IAdminModerationService>();
        _sut = new AdminModerationController(_adminModerationService);
    }

    #region GetAll

    [Test]
    public async Task GetAll_ReturnsOk()
    {
        var actions = new List<AdminModerationActionResponse>
        {
            new() { Id = 1, TargetType = "Restaurant", ActionType = "Approve" },
            new() { Id = 2, TargetType = "User", ActionType = "Ban" }
        };
        _adminModerationService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminModerationActionResponse>>.Success(actions));

        var result = await _sut.GetAll(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAll_WhenError_ReturnsError()
    {
        _adminModerationService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminModerationActionResponse>>.Failure(
                new ServiceError("Erreur", 500)));

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
        var action = new AdminModerationActionResponse { Id = 1, TargetType = "Restaurant" };
        _adminModerationService.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminModerationActionResponse>.Success(action));

        var result = await _sut.GetById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_WhenNotFound_Returns404()
    {
        _adminModerationService.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminModerationActionResponse>.Failure(
                new ServiceError("Action de modération introuvable", 404)));

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
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "1");

        var request = new AdminCreateModerationActionRequest
        {
            TargetType = ModerationTargetType.Restaurant,
            TargetId = 10,
            ActionType = ModerationActionType.Approve,
            Reason = "Conforme"
        };
        var response = new AdminModerationActionResponse { Id = 5, TargetType = "Restaurant" };
        _adminModerationService.CreateAsync(request, 1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminModerationActionResponse>.Success(response));

        var result = await _sut.Create(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
        var created = (CreatedAtActionResult)result;
        Assert.That(created.ActionName, Is.EqualTo(nameof(AdminModerationController.GetById)));
    }

    [Test]
    public async Task Create_WhenError_ReturnsError()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "1");

        var request = new AdminCreateModerationActionRequest
        {
            TargetType = ModerationTargetType.Restaurant,
            TargetId = 10,
            ActionType = ModerationActionType.Reject
        };
        _adminModerationService.CreateAsync(request, 1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminModerationActionResponse>.Failure(
                new ServiceError("Erreur", 400)));

        var result = await _sut.Create(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task Create_WhenUnauthenticated_ReturnsUnauthorized()
    {
        AuthenticationTestHelper.SetupUnauthenticatedUser(_sut);

        var request = new AdminCreateModerationActionRequest
        {
            TargetType = ModerationTargetType.Restaurant,
            TargetId = 10,
            ActionType = ModerationActionType.Approve
        };

        var result = await _sut.Create(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    #endregion
}
