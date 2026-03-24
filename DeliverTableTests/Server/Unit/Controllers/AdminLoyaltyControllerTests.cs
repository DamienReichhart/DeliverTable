using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminLoyaltyControllerTests
{
    private IAdminLoyaltyService _adminLoyaltyService = null!;
    private AdminLoyaltyController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _adminLoyaltyService = Substitute.For<IAdminLoyaltyService>();
        _sut = new AdminLoyaltyController(_adminLoyaltyService);
    }

    #region GetAllPrograms

    [Test]
    public async Task GetAllPrograms_ReturnsOk()
    {
        var programs = new List<AdminLoyaltyProgramResponse>
        {
            new() { Id = 1, PointsPerEuro = 1.0m },
            new() { Id = 2, PointsPerEuro = 2.0m }
        };
        _adminLoyaltyService.GetAllProgramsAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminLoyaltyProgramResponse>>.Success(programs));

        var result = await _sut.GetAllPrograms(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAllPrograms_WhenError_ReturnsError()
    {
        _adminLoyaltyService.GetAllProgramsAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminLoyaltyProgramResponse>>.Failure(new ServiceError("Erreur", 500)));

        var result = await _sut.GetAllPrograms(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region GetProgramById

    [Test]
    public async Task GetProgramById_WhenExists_ReturnsOk()
    {
        var program = new AdminLoyaltyProgramResponse { Id = 1, PointsPerEuro = 1.0m };
        _adminLoyaltyService.GetProgramByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminLoyaltyProgramResponse>.Success(program));

        var result = await _sut.GetProgramById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetProgramById_WhenNotFound_Returns404()
    {
        _adminLoyaltyService.GetProgramByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminLoyaltyProgramResponse>.Failure(
                new ServiceError("Programme de fidélité introuvable", 404)));

        var result = await _sut.GetProgramById(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region CreateProgram

    [Test]
    public async Task CreateProgram_WhenSuccess_ReturnsCreated()
    {
        var request = new AdminCreateLoyaltyProgramRequest
        {
            PointsPerEuro = 2.0m,
            EurosPerPoint = 0.05m,
            RestaurantId = 1,
            IsActive = true
        };
        var response = new AdminLoyaltyProgramResponse { Id = 10, PointsPerEuro = 2.0m };
        _adminLoyaltyService.CreateProgramAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminLoyaltyProgramResponse>.Success(response));

        var result = await _sut.CreateProgram(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
        var created = (CreatedAtActionResult)result;
        Assert.That(created.ActionName, Is.EqualTo(nameof(AdminLoyaltyController.GetProgramById)));
    }

    [Test]
    public async Task CreateProgram_WhenError_ReturnsError()
    {
        var request = new AdminCreateLoyaltyProgramRequest
        {
            PointsPerEuro = 1.0m,
            EurosPerPoint = 0.10m,
            RestaurantId = 99
        };
        _adminLoyaltyService.CreateProgramAsync(request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminLoyaltyProgramResponse>.Failure(
                new ServiceError("Etablissement introuvable", 404)));

        var result = await _sut.CreateProgram(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region UpdateProgram

    [Test]
    public async Task UpdateProgram_WhenSuccess_ReturnsOk()
    {
        var request = new AdminUpdateLoyaltyProgramRequest
        {
            PointsPerEuro = 3.0m,
            EurosPerPoint = 0.02m,
            IsActive = false
        };
        var response = new AdminLoyaltyProgramResponse { Id = 1, PointsPerEuro = 3.0m };
        _adminLoyaltyService.UpdateProgramAsync(1, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminLoyaltyProgramResponse>.Success(response));

        var result = await _sut.UpdateProgram(1, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task UpdateProgram_WhenNotFound_Returns404()
    {
        var request = new AdminUpdateLoyaltyProgramRequest
        {
            PointsPerEuro = 1.0m,
            EurosPerPoint = 0.10m
        };
        _adminLoyaltyService.UpdateProgramAsync(99, request, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminLoyaltyProgramResponse>.Failure(
                new ServiceError("Programme de fidélité introuvable", 404)));

        var result = await _sut.UpdateProgram(99, request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region DeleteProgram

    [Test]
    public async Task DeleteProgram_WhenSuccess_ReturnsNoContent()
    {
        _adminLoyaltyService.DeleteProgramAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.DeleteProgram(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteProgram_WhenNotFound_Returns404()
    {
        _adminLoyaltyService.DeleteProgramAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure(new ServiceError("Programme de fidélité introuvable", 404)));

        var result = await _sut.DeleteProgram(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region GetAccounts

    [Test]
    public async Task GetAccounts_WhenProgramExists_ReturnsOk()
    {
        var accounts = new List<AdminLoyaltyAccountResponse>
        {
            new() { Id = 1, PointsBalance = 100, CustomerName = "Jean Dupont" }
        };
        _adminLoyaltyService.GetAccountsAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminLoyaltyAccountResponse>>.Success(accounts));

        var result = await _sut.GetAccounts(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAccounts_WhenProgramNotFound_Returns404()
    {
        _adminLoyaltyService.GetAccountsAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminLoyaltyAccountResponse>>.Failure(
                new ServiceError("Programme de fidélité introuvable", 404)));

        var result = await _sut.GetAccounts(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region GetTransactions

    [Test]
    public async Task GetTransactions_ReturnsOk()
    {
        var transactions = new List<AdminLoyaltyTransactionResponse>
        {
            new() { Id = 1, Type = LoyaltyTransactionType.Earn, Points = 10 }
        };
        _adminLoyaltyService.GetTransactionsAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminLoyaltyTransactionResponse>>.Success(transactions));

        var result = await _sut.GetTransactions(1, 1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetTransactions_WhenError_ReturnsError()
    {
        _adminLoyaltyService.GetTransactionsAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminLoyaltyTransactionResponse>>.Failure(
                new ServiceError("Compte fidélité introuvable", 404)));

        var result = await _sut.GetTransactions(1, 99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion
}
