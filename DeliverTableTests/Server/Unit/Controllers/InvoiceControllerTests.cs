using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;
using DeliverTableTests.Global.Helpers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class InvoiceControllerTests
{
    private IInvoiceService _service = null!;
    private InvoiceController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IInvoiceService>();
        _sut = new InvoiceController(_service);
    }

    // ─── GetMine ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetMine_Authenticated_ReturnsPaginatedList()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "7", nameof(UserRole.Customer));
        var paginated = new PaginatedResult<InvoiceListItemDto>
        {
            Items =
            [
                new InvoiceListItemDto(
                    1,
                    "R0005-2026-000001",
                    InvoiceKind.OrderInvoiceToCustomer,
                    42,
                    DateTime.UtcNow,
                    20m,
                    "EUR",
                    InvoiceStatus.Generated),
            ],
            TotalCount = 1,
            Page = 1,
            PageSize = 20,
        };
        _service
            .ListForMeAsync(7, 1, 20, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<InvoiceListItemDto>>.Success(paginated));

        var result = await _sut.GetMine(1, 20, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.InstanceOf<PaginatedResult<InvoiceListItemDto>>());
    }

    [Test]
    public async Task GetMine_ServiceError_ReturnsError()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "7", nameof(UserRole.Customer));
        _service
            .ListForMeAsync(7, 1, 20, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<InvoiceListItemDto>>.Failure(
                new ServiceError("Erreur", 500)));

        var result = await _sut.GetMine(1, 20, CancellationToken.None);

        Assert.That(result, Is.Not.InstanceOf<OkObjectResult>());
    }

    // ─── GetForRestaurant ─────────────────────────────────────────────────

    [Test]
    public async Task GetForRestaurant_Owner_ReturnsPaginatedList()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "7", nameof(UserRole.RestaurantOwner));
        var paginated = new PaginatedResult<InvoiceListItemDto>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 20,
        };
        _service
            .ListForRestaurantAsync(5, 7, false, 1, 20, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<InvoiceListItemDto>>.Success(paginated));

        var result = await _sut.GetForRestaurant(5, 1, 20, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetForRestaurant_AccessDenied_ReturnsError()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "7", nameof(UserRole.RestaurantOwner));
        _service
            .ListForRestaurantAsync(5, 7, false, 1, 20, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<InvoiceListItemDto>>.Failure(
                new ServiceError("Vous n'êtes pas autorisé à consulter cette facture", 403)));

        var result = await _sut.GetForRestaurant(5, 1, 20, CancellationToken.None);

        Assert.That(result, Is.Not.InstanceOf<OkObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(403));
    }

    // ─── Download ─────────────────────────────────────────────────────────

    [Test]
    public async Task Download_NotGenerated_ReturnsConflict()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "7", nameof(UserRole.Customer));
        _service
            .GetPdfStreamAsync(1, 7, false, false, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<InvoicePdfStreamResult>.Failure(
                new ServiceError("La facture est en cours de génération, réessayez dans quelques instants", 409)));

        var result = await _sut.Download(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(409));
    }

    [Test]
    public async Task Download_Authorized_ReturnsFileResult()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "7", nameof(UserRole.Customer));
        var stream = new MemoryStream(new byte[] { 0x25, 0x50 });
        var payload = new InvoicePdfStreamResult(stream, "R0005-2026-000001.pdf", "application/pdf");
        _service
            .GetPdfStreamAsync(1, 7, false, false, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<InvoicePdfStreamResult>.Success(payload));

        var result = await _sut.Download(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<FileStreamResult>());
        var file = (FileStreamResult)result;
        Assert.That(file.ContentType, Is.EqualTo("application/pdf"));
        Assert.That(file.FileDownloadName, Is.EqualTo("R0005-2026-000001.pdf"));
    }

    [Test]
    public async Task Download_AccessDenied_ReturnsForbidden()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "7", nameof(UserRole.Customer));
        _service
            .GetPdfStreamAsync(1, 7, false, false, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<InvoicePdfStreamResult>.Failure(
                new ServiceError("Vous n'êtes pas autorisé à consulter cette facture", 403)));

        var result = await _sut.Download(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(403));
    }

    [Test]
    public async Task Download_NotFound_ReturnsNotFound()
    {
        AuthenticationTestHelper.SetupAuthenticatedUser(_sut, "7", nameof(UserRole.Customer));
        _service
            .GetPdfStreamAsync(1, 7, false, false, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<InvoicePdfStreamResult>.Failure(
                new ServiceError("Facture introuvable", 404)));

        var result = await _sut.Download(1, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(404));
    }
}
