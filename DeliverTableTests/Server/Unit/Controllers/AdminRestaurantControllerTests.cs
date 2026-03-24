using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminRestaurantControllerTests
{
    private IAdminRestaurantService _adminRestaurantService = null!;
    private AdminRestaurantController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _adminRestaurantService = Substitute.For<IAdminRestaurantService>();
        _sut = new AdminRestaurantController(_adminRestaurantService);
    }

    #region GetAll

    [Test]
    public async Task GetAll_ReturnsOk()
    {
        var restaurants = new List<AdminRestaurantResponse>
        {
            new() { Id = 1, Name = "Restaurant A" },
            new() { Id = 2, Name = "Restaurant B" }
        };
        _adminRestaurantService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminRestaurantResponse>>.Success(restaurants));

        var result = await _sut.GetAll(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAll_WhenError_ReturnsError()
    {
        _adminRestaurantService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminRestaurantResponse>>.Failure(new ServiceError("Erreur", 500)));

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
        var restaurant = new AdminRestaurantResponse { Id = 1, Name = "Restaurant A" };
        _adminRestaurantService.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminRestaurantResponse>.Success(restaurant));

        var result = await _sut.GetById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_WhenNotFound_Returns404()
    {
        _adminRestaurantService.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminRestaurantResponse>.Failure(new ServiceError("Etablissement introuvable", 404)));

        var result = await _sut.GetById(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region Update

    [Test]
    public async Task Update_WhenSuccess_ReturnsOk()
    {
        var request = new AdminUpdateRestaurantRequest
        {
            Name = "Updated",
            AdressLine1 = "1 Rue Test",
            City = "Paris",
            ZipCode = "75001",
            Country = "FR",
            IsActive = true
        };
        var response = new AdminRestaurantResponse { Id = 1, Name = "Updated" };
        _adminRestaurantService.UpdateAsync(1, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminRestaurantResponse>.Success(response));

        var result = await _sut.Update(1, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    #endregion

    #region Delete

    [Test]
    public async Task Delete_WhenSuccess_ReturnsNoContent()
    {
        _adminRestaurantService.DeleteAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.Delete(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    #endregion
}
