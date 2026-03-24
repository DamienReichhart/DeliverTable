using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminDiscountCodeControllerTests
{
    private IAdminDiscountCodeService _adminDiscountCodeService = null!;
    private AdminDiscountCodeController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _adminDiscountCodeService = Substitute.For<IAdminDiscountCodeService>();
        _sut = new AdminDiscountCodeController(_adminDiscountCodeService);
    }

    #region GetAll

    [Test]
    public async Task GetAll_ReturnsOk()
    {
        var codes = new List<AdminDiscountCodeResponse>
        {
            new() { Id = 1, Code = "CODE1" },
            new() { Id = 2, Code = "CODE2" }
        };
        _adminDiscountCodeService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminDiscountCodeResponse>>.Success(codes));

        var result = await _sut.GetAll(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAll_WhenError_ReturnsError()
    {
        _adminDiscountCodeService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminDiscountCodeResponse>>.Failure(new ServiceError("Erreur", 500)));

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
        var code = new AdminDiscountCodeResponse { Id = 1, Code = "CODE1" };
        _adminDiscountCodeService.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminDiscountCodeResponse>.Success(code));

        var result = await _sut.GetById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_WhenNotFound_Returns404()
    {
        _adminDiscountCodeService.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminDiscountCodeResponse>.Failure(new ServiceError("Code promo introuvable", 404)));

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
        var request = new AdminCreateDiscountCodeRequest
        {
            Code = "NEWCODE",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10m,
            ValidFrom = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            RestaurantId = 1,
            IsActive = true
        };
        var response = new AdminDiscountCodeResponse { Id = 10, Code = "NEWCODE" };
        _adminDiscountCodeService.CreateAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminDiscountCodeResponse>.Success(response));

        var result = await _sut.Create(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
        var created = (CreatedAtActionResult)result;
        Assert.That(created.ActionName, Is.EqualTo(nameof(AdminDiscountCodeController.GetById)));
    }

    [Test]
    public async Task Create_WhenError_ReturnsError()
    {
        var request = new AdminCreateDiscountCodeRequest
        {
            Code = "CODE",
            RestaurantId = 99,
            ValidFrom = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30)
        };
        _adminDiscountCodeService.CreateAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminDiscountCodeResponse>.Failure(new ServiceError("Restaurant introuvable", 404)));

        var result = await _sut.Create(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region Update

    [Test]
    public async Task Update_WhenSuccess_ReturnsOk()
    {
        var request = new AdminUpdateDiscountCodeRequest
        {
            Description = "Mis à jour",
            DiscountValue = 20m,
            ValidFrom = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };
        var response = new AdminDiscountCodeResponse { Id = 1, Description = "Mis à jour" };
        _adminDiscountCodeService.UpdateAsync(1, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminDiscountCodeResponse>.Success(response));

        var result = await _sut.Update(1, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Update_WhenNotFound_Returns404()
    {
        var request = new AdminUpdateDiscountCodeRequest
        {
            ValidFrom = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30)
        };
        _adminDiscountCodeService.UpdateAsync(99, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminDiscountCodeResponse>.Failure(new ServiceError("Code promo introuvable", 404)));

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
        _adminDiscountCodeService.DeleteAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_WhenNotFound_Returns404()
    {
        _adminDiscountCodeService.DeleteAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure(new ServiceError("Code promo introuvable", 404)));

        var result = await _sut.Delete(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region GetRedemptions

    [Test]
    public async Task GetRedemptions_WhenSuccess_ReturnsOk()
    {
        var redemptions = new List<AdminRedemptionResponse>
        {
            new() { Id = 1, CustomerName = "Test User", OrderId = 100 },
            new() { Id = 2, CustomerName = "Test User", OrderId = 101 }
        };
        _adminDiscountCodeService.GetRedemptionsAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminRedemptionResponse>>.Success(redemptions));

        var result = await _sut.GetRedemptions(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetRedemptions_WhenNotFound_Returns404()
    {
        _adminDiscountCodeService.GetRedemptionsAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminRedemptionResponse>>.Failure(new ServiceError("Code promo introuvable", 404)));

        var result = await _sut.GetRedemptions(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion
}
