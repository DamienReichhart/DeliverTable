using DeliverTableServer.Common;
using DeliverTableServer.Features.Admin;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminPromotionControllerTests
{
    private IAdminPromotionService _adminPromotionService = null!;
    private AdminPromotionController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _adminPromotionService = Substitute.For<IAdminPromotionService>();
        _sut = new AdminPromotionController(_adminPromotionService);
    }

    #region GetAll

    [Test]
    public async Task GetAll_ReturnsOk()
    {
        List<AdminPromotionResponse> promotions = new List<AdminPromotionResponse>
        {
            new() { Id = 1, Name = "Promo A" },
            new() { Id = 2, Name = "Promo B" }
        };
        _adminPromotionService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminPromotionResponse>>.Success(promotions));

        IActionResult result = await _sut.GetAll(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAll_WhenError_ReturnsError()
    {
        _adminPromotionService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminPromotionResponse>>.Failure(new ServiceError("Erreur", 500)));

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
        AdminPromotionResponse promotion = new AdminPromotionResponse { Id = 1, Name = "Promo A" };
        _adminPromotionService.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminPromotionResponse>.Success(promotion));

        IActionResult result = await _sut.GetById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_WhenNotFound_Returns404()
    {
        _adminPromotionService.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminPromotionResponse>.Failure(new ServiceError("Promotion introuvable", 404)));

        IActionResult result = await _sut.GetById(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        ObjectResult obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region Create

    [Test]
    public async Task Create_WhenSuccess_ReturnsCreated()
    {
        AdminCreatePromotionRequest request = new AdminCreatePromotionRequest
        {
            Name = "Nouvelle Promo",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10m,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30),
            RestaurantId = 1,
            IsActive = true
        };
        AdminPromotionResponse response = new AdminPromotionResponse { Id = 10, Name = "Nouvelle Promo" };
        _adminPromotionService.CreateAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminPromotionResponse>.Success(response));

        IActionResult result = await _sut.Create(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
        CreatedAtActionResult created = (CreatedAtActionResult)result;
        Assert.That(created.ActionName, Is.EqualTo(nameof(AdminPromotionController.GetById)));
    }

    [Test]
    public async Task Create_WhenError_ReturnsError()
    {
        AdminCreatePromotionRequest request = new AdminCreatePromotionRequest
        {
            Name = "Promo",
            RestaurantId = 99,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        };
        _adminPromotionService.CreateAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminPromotionResponse>.Failure(new ServiceError("Restaurant introuvable", 404)));

        IActionResult result = await _sut.Create(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        ObjectResult obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region Update

    [Test]
    public async Task Update_WhenSuccess_ReturnsOk()
    {
        AdminUpdatePromotionRequest request = new AdminUpdatePromotionRequest
        {
            Name = "Mis à jour",
            DiscountValue = 20m,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };
        AdminPromotionResponse response = new AdminPromotionResponse { Id = 1, Name = "Mis à jour" };
        _adminPromotionService.UpdateAsync(1, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminPromotionResponse>.Success(response));

        IActionResult result = await _sut.Update(1, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Update_WhenNotFound_Returns404()
    {
        AdminUpdatePromotionRequest request = new AdminUpdatePromotionRequest
        {
            Name = "Name",
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        };
        _adminPromotionService.UpdateAsync(99, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminPromotionResponse>.Failure(new ServiceError("Promotion introuvable", 404)));

        IActionResult result = await _sut.Update(99, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        ObjectResult obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region Delete

    [Test]
    public async Task Delete_WhenSuccess_ReturnsNoContent()
    {
        _adminPromotionService.DeleteAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        IActionResult result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_WhenNotFound_Returns404()
    {
        _adminPromotionService.DeleteAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure(new ServiceError("Promotion introuvable", 404)));

        IActionResult result = await _sut.Delete(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        ObjectResult obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion
}
