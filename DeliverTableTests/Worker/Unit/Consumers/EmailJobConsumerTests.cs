using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableInfrastructure.Services.Interfaces;
using DeliverTableSharedLibrary.Enums;
using DeliverTableWorker.Configuration;
using DeliverTableWorker.Consumers;
using DeliverTableWorker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableTests.Worker.Unit.Consumers;

[TestFixture]
public class EmailJobConsumerTests
{
    private IEmailSender _emailSender = null!;
    private IEmailTemplateRenderer _templateRenderer = null!;
    private IObjectStorageService _storage = null!;
    private IEmailJobRepository _emailJobRepo = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private WorkerEnvironment _env = null!;
    private EmailJobConsumer _sut = null!;

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        _emailSender = Substitute.For<IEmailSender>();
        _templateRenderer = Substitute.For<IEmailTemplateRenderer>();
        _storage = Substitute.For<IObjectStorageService>();
        _emailJobRepo = Substitute.For<IEmailJobRepository>();
        _env = BuildWorkerEnvironment();

        var services = new ServiceCollection();
        services.AddSingleton(_emailJobRepo);
        services.AddSingleton(_storage);
        _scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        _sut = new EmailJobConsumer(
            _scopeFactory,
            _emailSender,
            _templateRenderer,
            _env,
            NullLogger<EmailJobConsumer>.Instance);
    }

    [Test]
    public async Task ProcessJobAsync_WithAttachmentPath_DownloadsFromStorageAndAttaches()
    {
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        var job = new EmailJob
        {
            Id = 1,
            Type = EmailJobType.InvoiceReadyCustomer,
            Status = EmailJobStatus.Processing,
            RecipientEmail = "client@example.fr",
            RecipientName = "Jean Dupont",
            Subject = "Votre facture est disponible",
            TemplateData = "{}",
            AttachmentStoragePath = "invoices/2026/04/R0001-2026-000001.pdf",
            AttachmentFilename = "R0001-2026-000001.pdf",
        };

        _templateRenderer.RenderAsync(job.Type, job.TemplateData, Arg.Any<CancellationToken>())
            .Returns("<html>facture</html>");

        _storage.GetObjectAsync(job.AttachmentStoragePath, Arg.Any<CancellationToken>())
            .Returns(new ObjectStorageResult(
                Content: new MemoryStream(pdfBytes),
                ContentType: "application/pdf",
                ContentLength: pdfBytes.Length));

        using var scope = _scopeFactory.CreateScope();
        await _sut.ProcessJobAsync(job, scope, CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            job.RecipientEmail,
            job.RecipientName,
            job.Subject,
            "<html>facture</html>",
            Arg.Is<AttachmentPayload?>(a =>
                a != null
                && a.Filename == "R0001-2026-000001.pdf"
                && a.Bytes.SequenceEqual(pdfBytes)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessJobAsync_WithoutAttachment_SendsWithNoAttachment()
    {
        var job = new EmailJob
        {
            Id = 2,
            Type = EmailJobType.InvoiceReadyCustomer,
            Status = EmailJobStatus.Processing,
            RecipientEmail = "client@example.fr",
            RecipientName = "Jean Dupont",
            Subject = "Votre facture est disponible",
            TemplateData = "{}",
            AttachmentStoragePath = null,
            AttachmentFilename = null,
        };

        _templateRenderer.RenderAsync(job.Type, job.TemplateData, Arg.Any<CancellationToken>())
            .Returns("<html>facture</html>");

        using var scope = _scopeFactory.CreateScope();
        await _sut.ProcessJobAsync(job, scope, CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            job.RecipientEmail,
            job.RecipientName,
            job.Subject,
            "<html>facture</html>",
            null,
            Arg.Any<CancellationToken>());

        await _storage.DidNotReceive().GetObjectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessJobAsync_WithAttachmentPath_StorageReturnsNull_SendsWithNoAttachment()
    {
        var job = new EmailJob
        {
            Id = 3,
            Type = EmailJobType.InvoiceReadyCustomer,
            Status = EmailJobStatus.Processing,
            RecipientEmail = "client@example.fr",
            Subject = "Votre facture est disponible",
            TemplateData = "{}",
            AttachmentStoragePath = "invoices/2026/04/missing.pdf",
            AttachmentFilename = "missing.pdf",
        };

        _templateRenderer.RenderAsync(job.Type, job.TemplateData, Arg.Any<CancellationToken>())
            .Returns("<html>facture</html>");

        _storage.GetObjectAsync(job.AttachmentStoragePath, Arg.Any<CancellationToken>())
            .Returns((ObjectStorageResult?)null);

        using var scope = _scopeFactory.CreateScope();
        await _sut.ProcessJobAsync(job, scope, CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            job.RecipientEmail,
            job.RecipientName,
            job.Subject,
            "<html>facture</html>",
            null,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessJobAsync_WithAttachmentPath_NoFilename_UsesFilenameFromPath()
    {
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var job = new EmailJob
        {
            Id = 4,
            Type = EmailJobType.InvoiceReadyCustomer,
            Status = EmailJobStatus.Processing,
            RecipientEmail = "client@example.fr",
            Subject = "Votre facture est disponible",
            TemplateData = "{}",
            AttachmentStoragePath = "invoices/2026/04/R0001-2026-000001.pdf",
            AttachmentFilename = null, // No explicit filename
        };

        _templateRenderer.RenderAsync(job.Type, job.TemplateData, Arg.Any<CancellationToken>())
            .Returns("<html>facture</html>");

        _storage.GetObjectAsync(job.AttachmentStoragePath, Arg.Any<CancellationToken>())
            .Returns(new ObjectStorageResult(
                Content: new MemoryStream(pdfBytes),
                ContentType: "application/pdf",
                ContentLength: pdfBytes.Length));

        using var scope = _scopeFactory.CreateScope();
        await _sut.ProcessJobAsync(job, scope, CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            Arg.Is(job.RecipientEmail),
            Arg.Is<string?>(n => n == null),
            Arg.Is(job.Subject),
            Arg.Any<string>(),
            Arg.Is<AttachmentPayload?>(a => a != null && a.Filename == "R0001-2026-000001.pdf"),
            Arg.Any<CancellationToken>());
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
