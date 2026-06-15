using DeliverTableServer.Common;
using DeliverTableServer.Features.Admin;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminDishControllerTests
{
    private IAdminDishService _adminDishService = null!;
    private AdminDishController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _adminDishService = Substitute.For<IAdminDishService>();
        _sut = new AdminDishController(_adminDishService);
    }

    #region GetAll

    [Test]
    public async Task GetAll_ReturnsOk()
    {
        List<AdminDishResponse> dishes = new List<AdminDishResponse>
        {
            new() { Id = 1, Name = "Plat A" },
            new() { Id = 2, Name = "Plat B" }
        };
        _adminDishService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminDishResponse>>.Success(dishes));

        IActionResult result = await _sut.GetAll(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAll_WhenError_ReturnsError()
    {
        _adminDishService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminDishResponse>>.Failure(new ServiceError("Erreur", 500)));

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
        AdminDishResponse dish = new AdminDishResponse { Id = 1, Name = "Plat A" };
        _adminDishService.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminDishResponse>.Success(dish));

        IActionResult result = await _sut.GetById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_WhenNotFound_Returns404()
    {
        _adminDishService.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminDishResponse>.Failure(new ServiceError("Plat introuvable", 404)));

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
        AdminCreateDishRequest request = new AdminCreateDishRequest
        {
            Name = "Nouveau Plat",
            BasePrice = 12.50m,
            RestaurantId = 1,
            IsActive = true
        };
        AdminDishResponse response = new AdminDishResponse { Id = 10, Name = "Nouveau Plat" };
        _adminDishService.CreateAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminDishResponse>.Success(response));

        IActionResult result = await _sut.Create(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
        CreatedAtActionResult created = (CreatedAtActionResult)result;
        Assert.That(created.ActionName, Is.EqualTo(nameof(AdminDishController.GetById)));
    }

    #endregion

    #region Update

    [Test]
    public async Task Update_WhenSuccess_ReturnsOk()
    {
        AdminUpdateDishRequest request = new AdminUpdateDishRequest
        {
            Name = "Mis à jour",
            BasePrice = 15.00m,
            IsActive = true
        };
        AdminDishResponse response = new AdminDishResponse { Id = 1, Name = "Mis à jour" };
        _adminDishService.UpdateAsync(1, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminDishResponse>.Success(response));

        IActionResult result = await _sut.Update(1, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    #endregion

    #region Delete

    [Test]
    public async Task Delete_WhenSuccess_ReturnsNoContent()
    {
        _adminDishService.DeleteAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        IActionResult result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    #endregion
}
