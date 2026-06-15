using DeliverTableServer.Common;
using DeliverTableServer.Features.Admin;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminTransactionControllerTests
{
    private IAdminTransactionService _adminTransactionService = null!;
    private AdminTransactionController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _adminTransactionService = Substitute.For<IAdminTransactionService>();
        _sut = new AdminTransactionController(_adminTransactionService);
    }

    #region GetAll

    [Test]
    public async Task GetAll_ReturnsOk()
    {
        List<AdminTransactionResponse> transactions = new List<AdminTransactionResponse>
        {
            new() { Id = 1, Type = "OrderCommission" },
            new() { Id = 2, Type = "Withdrawal" }
        };
        _adminTransactionService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminTransactionResponse>>.Success(transactions));

        IActionResult result = await _sut.GetAll(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAll_WhenError_ReturnsError()
    {
        _adminTransactionService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<AdminTransactionResponse>>.Failure(new ServiceError("Erreur", 500)));

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
        AdminTransactionResponse transaction = new AdminTransactionResponse { Id = 1, Type = "OrderCommission" };
        _adminTransactionService.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminTransactionResponse>.Success(transaction));

        IActionResult result = await _sut.GetById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_WhenNotFound_Returns404()
    {
        _adminTransactionService.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminTransactionResponse>.Failure(new ServiceError("Transaction introuvable", 404)));

        IActionResult result = await _sut.GetById(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        ObjectResult obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }

    #endregion
}
