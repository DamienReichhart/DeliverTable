using System.Reflection;
using DeliverTableServer.Common;
using DeliverTableServer.Features.Admin;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminInvoiceControllerTests
{
    private IInvoiceService _invoiceService = null!;
    private AdminInvoiceController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _invoiceService = Substitute.For<IInvoiceService>();
        _sut = new AdminInvoiceController(_invoiceService);
    }

    #region List

    [Test]
    public async Task List_ReturnsOkWithPaginatedResult()
    {
        AdminInvoiceRowDto row = new AdminInvoiceRowDto(
            1,
            "DT-2026-000001",
            InvoiceKind.CommissionInvoiceToRestaurant,
            InvoiceIssuerType.Platform,
            "Platform",
            "Restaurant XYZ",
            new DateTime(2026, 1, 15),
            2.40m,
            InvoiceStatus.Generated);

        PaginatedResult<AdminInvoiceRowDto> paginated = new PaginatedResult<AdminInvoiceRowDto>
        {
            Items = [row],
            TotalCount = 1,
            Page = 1,
            PageSize = 20,
        };

        _invoiceService
            .AdminListAsync(Arg.Any<InvoiceAdminQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<AdminInvoiceRowDto>>.Success(paginated));

        IActionResult result = await _sut.List(new InvoiceAdminQuery(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task List_WhenServiceFails_ReturnsError()
    {
        _invoiceService
            .AdminListAsync(Arg.Any<InvoiceAdminQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<AdminInvoiceRowDto>>.Failure(
                new ServiceError("Erreur interne", 500)));

        IActionResult result = await _sut.List(new InvoiceAdminQuery(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region GetById

    [Test]
    public async Task GetById_WhenExists_ReturnsOk()
    {
        InvoiceListItemDto header = new InvoiceListItemDto(
            1, "DT-2026-000001", InvoiceKind.CommissionInvoiceToRestaurant,
            42, DateTime.UtcNow, 2.40m, "EUR", InvoiceStatus.Generated);
        InvoiceLegalSnapshotDto issuer = new InvoiceLegalSnapshotDto("Platform", "SAS", "99999", "FR12345", "10 rue");
        InvoiceLegalSnapshotDto recipient = new InvoiceLegalSnapshotDto("Restaurant", "SARL", "11111", "", "2 av");
        AdminInvoiceDetailDto detail = new AdminInvoiceDetailDto(header, [], issuer, recipient, null);

        _invoiceService
            .AdminGetDetailAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminInvoiceDetailDto>.Success(detail));

        IActionResult result = await _sut.GetById(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_WhenNotFound_Returns404()
    {
        _invoiceService
            .AdminGetDetailAsync(99, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminInvoiceDetailDto>.Failure(
                new ServiceError("Facture introuvable", 404)));

        IActionResult result = await _sut.GetById(99, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(404));
    }

    #endregion

    #region ResendEmail

    [Test]
    public async Task ResendEmail_WhenSuccess_ReturnsNoContent()
    {
        _invoiceService
            .AdminResendEmailAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        IActionResult result = await _sut.ResendEmail(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task ResendEmail_WhenNotGenerated_Returns409()
    {
        _invoiceService
            .AdminResendEmailAsync(1, Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure(new ServiceError("La facture est en cours de génération", 409)));

        IActionResult result = await _sut.ResendEmail(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(409));
    }

    [Test]
    public void AdminInvoiceController_HasAdministratorRoleAttribute()
    {
        List<AuthorizeAttribute> classAttrs = typeof(AdminInvoiceController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .ToList();

        Assert.That(
            classAttrs.Any(a => a.Roles?.Contains(nameof(UserRole.Administrator)) == true),
            Is.True);
    }

    #endregion
}
