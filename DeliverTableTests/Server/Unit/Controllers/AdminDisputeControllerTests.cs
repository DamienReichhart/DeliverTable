using DeliverTableServer.Common;
using DeliverTableServer.Features.Admin;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dispute;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminDisputeControllerTests
{
    private IDisputeService _service = null!;
    private AdminDisputeController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IDisputeService>();
        _sut = new AdminDisputeController(_service);
    }

    [Test]
    public async Task List_ReturnsOkWithPaginatedRows()
    {
        DisputeAdminFilter filter = new DisputeAdminFilter { State = DisputeState.Open, Page = 1, PageSize = 20 };
        PaginatedResult<AdminDisputeRowDto> paginated = new PaginatedResult<AdminDisputeRowDto>
        {
            Items =
            [
                new AdminDisputeRowDto(
                    1, "dp_1", 10, 5, "Chez Toto", "c@d.fr",
                    25m, "EUR", "fraudulent",
                    DisputeState.Open, DateTime.UtcNow, null, DateTime.UtcNow.AddDays(7)),
            ],
            TotalCount = 1,
            Page = 1,
            PageSize = 20,
        };
        _service.ListForAdminAsync(filter, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<AdminDisputeRowDto>>.Success(paginated));

        IActionResult result = await _sut.List(filter, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        OkObjectResult ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.InstanceOf<PaginatedResult<AdminDisputeRowDto>>());
    }

    [Test]
    public async Task GetById_Found_ReturnsOkWithDetail()
    {
        AdminDisputeRowDto header = new AdminDisputeRowDto(
            1, "dp_1", 10, 5, "Chez Toto", "c@d.fr",
            25m, "EUR", "fraudulent",
            DisputeState.Open, DateTime.UtcNow, null, DateTime.UtcNow.AddDays(7));
        AdminDisputeDetailDto detail = new AdminDisputeDetailDto(
            header,
            "https://dashboard.stripe.com/test/disputes/dp_1",
            42, "ch_1", 100m,
            new List<RestaurantTransactionDto>());
        _service.GetAdminDetailAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminDisputeDetailDto>.Success(detail));

        IActionResult result = await _sut.GetById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        OkObjectResult ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.EqualTo(detail));
    }

    [Test]
    public async Task GetById_NotFound_ReturnsError()
    {
        _service.GetAdminDetailAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminDisputeDetailDto>.Failure(
                new ServiceError("Not found", 404)));

        IActionResult result = await _sut.GetById(99, CancellationToken.None);

        Assert.That(result, Is.Not.InstanceOf<OkObjectResult>());
    }
}
