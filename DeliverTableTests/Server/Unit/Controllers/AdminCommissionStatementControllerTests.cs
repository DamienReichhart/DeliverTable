using DeliverTableServer.Common;
using DeliverTableServer.Features.Admin;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.CommissionStatement;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminCommissionStatementControllerTests
{
    private ICommissionStatementService _service = null!;
    private AdminCommissionStatementController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<ICommissionStatementService>();
        _sut = new AdminCommissionStatementController(_service);
    }

    [Test]
    public async Task Run_DefaultsToPreviousMonth_WhenBodyOmitted()
    {
        DateTime nowParis = new DateTime(2026, 6, 5, 9, 0, 0, DateTimeKind.Utc); // mid-June UTC
        _sut.UtcNowOverride = nowParis;
        CommissionStatementGenerationResultDto expectedDto = new CommissionStatementGenerationResultDto { PeriodYear = 2026, PeriodMonth = 5 };
        _service.GenerateForPeriodAsync(2026, 5, default)
                .ReturnsForAnyArgs(ServiceResult<CommissionStatementGenerationResultDto>.Success(expectedDto));

        IActionResult result = await _sut.Run(body: null, default);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        await _service.Received(1).GenerateForPeriodAsync(2026, 5, default);
    }

    [Test]
    public async Task Run_UsesProvidedPeriod()
    {
        _service.GenerateForPeriodAsync(2026, 3, default)
                .Returns(ServiceResult<CommissionStatementGenerationResultDto>.Success(new()));

        IActionResult result = await _sut.Run(new CommissionStatementsRunRequest { Year = 2026, Month = 3 }, default);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        await _service.Received(1).GenerateForPeriodAsync(2026, 3, default);
    }

    [Test]
    public async Task List_DelegatesFiltersToService_AndReturnsOk()
    {
        PaginatedResult<AdminCommissionStatementRowDto> expected = new PaginatedResult<AdminCommissionStatementRowDto>
        {
            Items = [new AdminCommissionStatementRowDto { Id = 1, Number = "COMM-2026-04-000001" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 50,
        };
        _service.AdminListAsync(2026, CommissionStatementKind.Invoice, null, 1, 50, default)
                .ReturnsForAnyArgs(ServiceResult<PaginatedResult<AdminCommissionStatementRowDto>>.Success(expected));

        IActionResult result = await _sut.List(year: 2026, kind: CommissionStatementKind.Invoice, restaurantId: null, page: 1, pageSize: 50, default);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        await _service.Received(1).AdminListAsync(2026, CommissionStatementKind.Invoice, null, 1, 50, default);
    }

    [Test]
    public async Task GetById_ReturnsOk_WhenStatementFound()
    {
        AdminCommissionStatementDetailDto detail = new AdminCommissionStatementDetailDto { Id = 5, Number = "COMM-2026-04-000005" };
        _service.AdminGetDetailAsync(5, default)
                .ReturnsForAnyArgs(ServiceResult<AdminCommissionStatementDetailDto>.Success(detail));

        IActionResult result = await _sut.GetById(5, default);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        await _service.Received(1).AdminGetDetailAsync(5, default);
    }

    [Test]
    public async Task GetById_ReturnsNotFound_WhenStatementMissing()
    {
        _service.AdminGetDetailAsync(99, default)
                .ReturnsForAnyArgs(ServiceError.NotFound("Relevé de commissions introuvable"));

        IActionResult result = await _sut.GetById(99, default);

        ObjectResult? objectResult = result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task GetPdf_ReturnsFile_WhenPdfExists()
    {
        byte[] pdfBytes = new byte[] { 1, 2, 3 };
        _service.AdminGetPdfAsync(7, default)
                .ReturnsForAnyArgs(ServiceResult<(byte[] Pdf, string FileName)>.Success((pdfBytes, "COMM-2026-04-000007.pdf")));

        IActionResult result = await _sut.GetPdf(7, default);

        Assert.That(result, Is.InstanceOf<FileContentResult>());
        FileContentResult fileResult = (FileContentResult)result;
        Assert.That(fileResult.FileContents, Is.EqualTo(pdfBytes));
        Assert.That(fileResult.FileDownloadName, Is.EqualTo("COMM-2026-04-000007.pdf"));
    }

    [Test]
    public async Task GetPdf_ReturnsNotFound_WhenPdfNotGenerated()
    {
        _service.AdminGetPdfAsync(7, default)
                .ReturnsForAnyArgs(ServiceError.NotFound("PDF non encore généré"));

        IActionResult result = await _sut.GetPdf(7, default);

        ObjectResult? objectResult = result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult!.StatusCode, Is.EqualTo(404));
    }
}
