using System.Text.Json;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableInfrastructure.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;
using DeliverTableWorker.Consumers;
using DeliverTableWorker.Configuration;
using DeliverTableWorker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using QuestPDF.Infrastructure;

namespace DeliverTableTests.Worker.Unit.Consumers;

[TestFixture]
public class InvoiceJobConsumerTests
{
    private IInvoiceRepository _invoiceRepo = null!;
    private IInvoicePdfRenderer _renderer = null!;
    private IObjectStorageService _storage = null!;
    private IEmailJobRepository _emailJobRepo = null!;
    private IMessagePublisher _publisher = null!;
    private WorkerEnvironment _env = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private InvoiceJobConsumer _sut = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        _invoiceRepo = Substitute.For<IInvoiceRepository>();
        _renderer = Substitute.For<IInvoicePdfRenderer>();
        _storage = Substitute.For<IObjectStorageService>();
        _emailJobRepo = Substitute.For<IEmailJobRepository>();
        _publisher = Substitute.For<IMessagePublisher>();
        _env = BuildWorkerEnvironment();

        var services = new ServiceCollection();
        services.AddSingleton(_invoiceRepo);
        services.AddSingleton(_renderer);
        services.AddSingleton(_storage);
        services.AddSingleton(_emailJobRepo);
        _scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        _sut = new InvoiceJobConsumer(
            _scopeFactory,
            _publisher,
            _env,
            NullLogger<InvoiceJobConsumer>.Instance);
    }

    [Test]
    public async Task HandleAsync_HappyPath_GeneratesUploadsAndQueuesEmail()
    {
        var recipientSnapshot = new InvoiceLegalSnapshotDto(
            Name: "Jean Dupont",
            LegalForm: "",
            Siret: "",
            VatNumber: "",
            Address: "client@example.fr");

        var invoice = new Invoice
        {
            Id = 1,
            Number = "R0001-2026-000001",
            Kind = InvoiceKind.OrderInvoiceToCustomer,
            OrderId = 42,
            IssuedAt = new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc),
            Currency = "EUR",
            Status = InvoiceStatus.Queued,
            TotalTtc = 22m,
            TotalHt = 20m,
            TotalVat = 2m,
            IssuerLegalSnapshotJson = JsonSerializer.Serialize(new InvoiceLegalSnapshotDto("Resto SAS", "SAS", "73282932000074", "", "1 rue")),
            RecipientSnapshotJson = JsonSerializer.Serialize(recipientSnapshot),
            Lines = new List<InvoiceLine>
            {
                new() { Description = "Plat", Quantity = 2m, UnitPriceHt = 10m, UnitPriceTtc = 11m, VatRate = 10m, LineHt = 20m, LineVat = 2m, LineTtc = 22m, SortOrder = 0 },
            },
        };

        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        var storageKey = "invoices/2026/04/R0001-2026-000001.pdf";

        _invoiceRepo.GetByIdWithLinesAndRecipientsAsync(1, Arg.Any<CancellationToken>()).Returns(invoice);
        _renderer.Render(invoice).Returns(pdfBytes);
        _storage.UploadAsync(pdfBytes, "application/pdf", "invoices/2026/04", "R0001-2026-000001.pdf", Arg.Any<CancellationToken>())
                .Returns(storageKey);

        await _sut.HandleAsync(new InvoiceJobMessage(1), CancellationToken.None);

        Assert.That(invoice.Status, Is.EqualTo(InvoiceStatus.Generated));
        Assert.That(invoice.StoragePath, Is.EqualTo(storageKey));
        Assert.That(invoice.FailureReason, Is.Null);

        await _invoiceRepo.Received(1).UpdateAsync(
            Arg.Is<Invoice>(i => i.Status == InvoiceStatus.Generated && i.StoragePath == storageKey),
            Arg.Any<CancellationToken>());

        await _emailJobRepo.Received(1).CreateAsync(
            Arg.Is<EmailJob>(j =>
                j.Type == EmailJobType.InvoiceReadyCustomer
                && j.RecipientEmail == "client@example.fr"
                && j.AttachmentStoragePath == storageKey
                && j.AttachmentFilename == "R0001-2026-000001.pdf"),
            Arg.Any<CancellationToken>());

        await _publisher.Received(1).PublishAsync(
            "email",
            Arg.Any<EmailJobMessage>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_RendererThrows_MarksFailedAndRethrows()
    {
        var recipientSnapshot = new InvoiceLegalSnapshotDto("Jean Dupont", "", "", "", "client@example.fr");

        var invoice = new Invoice
        {
            Id = 2,
            Number = "R0001-2026-000002",
            Kind = InvoiceKind.OrderInvoiceToCustomer,
            OrderId = 43,
            IssuedAt = DateTime.UtcNow,
            Currency = "EUR",
            Status = InvoiceStatus.Queued,
            TotalTtc = 10m,
            TotalHt = 9m,
            TotalVat = 1m,
            IssuerLegalSnapshotJson = JsonSerializer.Serialize(new InvoiceLegalSnapshotDto("Resto", "SAS", "", "", "")),
            RecipientSnapshotJson = JsonSerializer.Serialize(recipientSnapshot),
            Lines = [],
        };

        _invoiceRepo.GetByIdWithLinesAndRecipientsAsync(2, Arg.Any<CancellationToken>()).Returns(invoice);
        _renderer.Render(invoice).Throws(new Exception("render failed"));

        Assert.ThrowsAsync<Exception>(() => _sut.HandleAsync(new InvoiceJobMessage(2), CancellationToken.None));

        await _invoiceRepo.Received(1).UpdateAsync(
            Arg.Is<Invoice>(i => i.Status == InvoiceStatus.Failed && i.FailureReason == "render failed"),
            Arg.Any<CancellationToken>());

        await _publisher.DidNotReceive().PublishAsync(
            Arg.Any<string>(),
            Arg.Any<EmailJobMessage>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_InvoiceNotFound_LogsWarningAndReturns()
    {
        _invoiceRepo.GetByIdWithLinesAndRecipientsAsync(99, Arg.Any<CancellationToken>()).Returns((Invoice?)null);

        await _sut.HandleAsync(new InvoiceJobMessage(99), CancellationToken.None);

        _renderer.DidNotReceive().Render(Arg.Any<Invoice>());
        await _storage.DidNotReceive().UploadAsync(
            Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<EmailJobMessage>(), Arg.Any<CancellationToken>());
    }

    private static WorkerEnvironment BuildWorkerEnvironment()
    {
        var vars = new Dictionary<string, string>
        {
            ["CONNECTION_STRING_DATABASE"] = "Host=localhost;Database=test",
            ["RABBITMQ_HOST"] = "localhost",
            ["RABBITMQ_USER"] = "guest",
            ["RABBITMQ_PASSWORD"] = "guest",
            ["SMTP_HOST"] = "smtp.test.local",
            ["SMTP_USER"] = "user@test.local",
            ["SMTP_PASSWORD"] = "password",
            ["SMTP_FROM_EMAIL"] = "noreply@test.local",
            ["PLATFORM_LEGAL_NAME"] = "Test Platform SAS",
            ["PLATFORM_LEGAL_FORM"] = "SAS",
            ["PLATFORM_SIRET"] = "73282932000074",
            ["PLATFORM_VAT_NUMBER"] = "FR12345678900",
            ["PLATFORM_ADDRESS"] = "1 rue Test, 75001 Paris",
            ["OBJECT_STORAGE_SERVICE_URL"] = "http://localhost:3900",
            ["OBJECT_STORAGE_ACCESS_KEY"] = "key",
            ["OBJECT_STORAGE_SECRET_KEY"] = "secret",
            ["OBJECT_STORAGE_BUCKET_NAME"] = "bucket",
        };

        foreach (var (key, value) in vars)
            Environment.SetEnvironmentVariable(key, value);

        try
        {
            return WorkerEnvironment.Load();
        }
        finally
        {
            foreach (var key in vars.Keys)
                Environment.SetEnvironmentVariable(key, null);
        }
    }
}
