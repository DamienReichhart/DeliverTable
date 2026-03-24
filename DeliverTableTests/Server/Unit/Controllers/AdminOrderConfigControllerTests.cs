using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminOrderConfigControllerTests
{
    private IAdminOrderConfigService _adminOrderConfigService = null!;
    private AdminOrderConfigController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _adminOrderConfigService = Substitute.For<IAdminOrderConfigService>();
        _sut = new AdminOrderConfigController(_adminOrderConfigService);
    }

    #region GetAllRules

    [Test]
    public async Task GetAllRules_ReturnsOk()
    {
        var rules = new List<AdminOrderRuleResponse>
        {
            new() { Id = 1, RestaurantId = 1 },
            new() { Id = 2, RestaurantId = 2 }
        };
        _adminOrderConfigService.GetAllRulesAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminOrderRuleResponse>>.Success(rules));

        var result = await _sut.GetAllRules(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAllRules_WhenError_ReturnsError()
    {
        _adminOrderConfigService.GetAllRulesAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminOrderRuleResponse>>.Failure(new ServiceError("Erreur", 500)));

        var result = await _sut.GetAllRules(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region GetRuleById

    [Test]
    public async Task GetRuleById_WhenExists_ReturnsOk()
    {
        var rule = new AdminOrderRuleResponse { Id = 1, RestaurantId = 1 };
        _adminOrderConfigService.GetRuleByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderRuleResponse>.Success(rule));

        var result = await _sut.GetRuleById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetRuleById_WhenNotFound_Returns404()
    {
        _adminOrderConfigService.GetRuleByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderRuleResponse>.Failure(
                new ServiceError("Règle de commande introuvable", 404)));

        var result = await _sut.GetRuleById(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region CreateRule

    [Test]
    public async Task CreateRule_WhenSuccess_ReturnsCreated()
    {
        var request = new AdminCreateOrderRuleRequest
        {
            RestaurantId = 1,
            AllowPreorder = true
        };
        var response = new AdminOrderRuleResponse { Id = 10, RestaurantId = 1, AllowPreorder = true };
        _adminOrderConfigService.CreateRuleAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderRuleResponse>.Success(response));

        var result = await _sut.CreateRule(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
        var created = (CreatedAtActionResult)result;
        Assert.That(created.ActionName, Is.EqualTo(nameof(AdminOrderConfigController.GetRuleById)));
    }

    [Test]
    public async Task CreateRule_WhenError_ReturnsError()
    {
        var request = new AdminCreateOrderRuleRequest { RestaurantId = 99 };
        _adminOrderConfigService.CreateRuleAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderRuleResponse>.Failure(
                new ServiceError("Etablissement introuvable", 404)));

        var result = await _sut.CreateRule(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region UpdateRule

    [Test]
    public async Task UpdateRule_WhenSuccess_ReturnsOk()
    {
        var request = new AdminUpdateOrderRuleRequest { AllowPreorder = true };
        var response = new AdminOrderRuleResponse { Id = 1, AllowPreorder = true };
        _adminOrderConfigService.UpdateRuleAsync(1, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderRuleResponse>.Success(response));

        var result = await _sut.UpdateRule(1, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task UpdateRule_WhenNotFound_Returns404()
    {
        var request = new AdminUpdateOrderRuleRequest { AllowPreorder = true };
        _adminOrderConfigService.UpdateRuleAsync(99, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderRuleResponse>.Failure(
                new ServiceError("Règle de commande introuvable", 404)));

        var result = await _sut.UpdateRule(99, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region DeleteRule

    [Test]
    public async Task DeleteRule_WhenSuccess_ReturnsNoContent()
    {
        _adminOrderConfigService.DeleteRuleAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.DeleteRule(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteRule_WhenNotFound_Returns404()
    {
        _adminOrderConfigService.DeleteRuleAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure(new ServiceError("Règle de commande introuvable", 404)));

        var result = await _sut.DeleteRule(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region GetAllBlockedSlots

    [Test]
    public async Task GetAllBlockedSlots_ReturnsOk()
    {
        var slots = new List<AdminBlockedSlotResponse>
        {
            new() { Id = 1, RestaurantId = 1 },
            new() { Id = 2, RestaurantId = 1 }
        };
        _adminOrderConfigService.GetAllBlockedSlotsAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminBlockedSlotResponse>>.Success(slots));

        var result = await _sut.GetAllBlockedSlots(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAllBlockedSlots_WhenError_ReturnsError()
    {
        _adminOrderConfigService.GetAllBlockedSlotsAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminBlockedSlotResponse>>.Failure(new ServiceError("Erreur", 500)));

        var result = await _sut.GetAllBlockedSlots(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region GetBlockedSlotById

    [Test]
    public async Task GetBlockedSlotById_WhenExists_ReturnsOk()
    {
        var slot = new AdminBlockedSlotResponse { Id = 1, RestaurantId = 1 };
        _adminOrderConfigService.GetBlockedSlotByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminBlockedSlotResponse>.Success(slot));

        var result = await _sut.GetBlockedSlotById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetBlockedSlotById_WhenNotFound_Returns404()
    {
        _adminOrderConfigService.GetBlockedSlotByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminBlockedSlotResponse>.Failure(
                new ServiceError("Créneau bloqué introuvable", 404)));

        var result = await _sut.GetBlockedSlotById(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region CreateBlockedSlot

    [Test]
    public async Task CreateBlockedSlot_WhenSuccess_ReturnsCreated()
    {
        var request = new AdminCreateBlockedSlotRequest
        {
            RestaurantId = 1,
            StartsAt = DateTime.UtcNow.AddDays(1),
            EndsAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            Reason = "Maintenance"
        };
        var response = new AdminBlockedSlotResponse { Id = 10, RestaurantId = 1, Reason = "Maintenance" };
        _adminOrderConfigService.CreateBlockedSlotAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminBlockedSlotResponse>.Success(response));

        var result = await _sut.CreateBlockedSlot(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
        var created = (CreatedAtActionResult)result;
        Assert.That(created.ActionName, Is.EqualTo(nameof(AdminOrderConfigController.GetBlockedSlotById)));
    }

    [Test]
    public async Task CreateBlockedSlot_WhenError_ReturnsError()
    {
        var request = new AdminCreateBlockedSlotRequest
        {
            RestaurantId = 1,
            StartsAt = DateTime.UtcNow.AddDays(2),
            EndsAt = DateTime.UtcNow.AddDays(1)
        };
        _adminOrderConfigService.CreateBlockedSlotAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminBlockedSlotResponse>.Failure(
                new ServiceError("Dates invalides", 400)));

        var result = await _sut.CreateBlockedSlot(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(400));
    }

    #endregion

    #region DeleteBlockedSlot

    [Test]
    public async Task DeleteBlockedSlot_WhenSuccess_ReturnsNoContent()
    {
        _adminOrderConfigService.DeleteBlockedSlotAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.DeleteBlockedSlot(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteBlockedSlot_WhenNotFound_Returns404()
    {
        _adminOrderConfigService.DeleteBlockedSlotAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure(new ServiceError("Créneau bloqué introuvable", 404)));

        var result = await _sut.DeleteBlockedSlot(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion
}
