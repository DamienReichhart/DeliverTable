using System.Text;
using System.Text.Json;
using DeliverTableInfrastructure.Extensions;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableInfrastructure.Services.Interfaces;
using DeliverTableInfrastructure.TemplateData;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;
using DeliverTableWorker.Configuration;
using DeliverTableWorker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DeliverTableWorker.Consumers;

public sealed class InvoiceJobConsumer(
    IServiceScopeFactory scopeFactory,
    IMessagePublisher publisher,
    WorkerEnvironment env,
    ILogger<InvoiceJobConsumer> logger) : BackgroundService
{
    private const string MainExchange = "delivertable.jobs";
    private const string MainQueue = "delivertable.jobs.invoice";
    private const string RoutingKey = MessagingExchanges.Invoice;

    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RabbitMQ connection lost. Reconnecting in 5 seconds...");
                await CleanupAsync();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        await CleanupAsync();
    }

    private async Task ConnectAndConsumeAsync(CancellationToken ct)
    {
        ConnectionFactory factory = new ConnectionFactory
        {
            HostName = env.RabbitMqHost,
            Port = env.RabbitMqPort,
            UserName = env.RabbitMqUser,
            Password = env.RabbitMqPassword,
        };

        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

        await _channel.BasicQosAsync(0, 1, false, ct);
        await DeclareTopologyAsync(ct);

        AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                await HandleMessageAsync(ea, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Unhandled error processing invoice message {DeliveryTag}",
                    ea.DeliveryTag
                );
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: MainQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct
        );

        logger.LogInformation("InvoiceJobConsumer started, listening on {Queue}", MainQueue);

        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    private async Task DeclareTopologyAsync(CancellationToken ct)
    {
        IChannel channel = _channel ?? throw new InvalidOperationException("Channel not initialized");

        await channel.ExchangeDeclareAsync(
            exchange: MainExchange,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: ct
        );

        await channel.QueueDeclareAsync(
            queue: MainQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct
        );

        await channel.QueueBindAsync(MainQueue, MainExchange, RoutingKey, cancellationToken: ct);
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        string json = Encoding.UTF8.GetString(ea.Body.ToArray());
        InvoiceJobMessage? message = JsonSerializer.Deserialize<InvoiceJobMessage>(json);

        if (message is null)
        {
            logger.LogWarning("Received null or invalid invoice message, acking and skipping");
            await _channel!.BasicAckAsync(ea.DeliveryTag, false, ct);
            return;
        }

        try
        {
            await HandleAsync(message, ct);
            await _channel!.BasicAckAsync(ea.DeliveryTag, false, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process invoice job for InvoiceId {InvoiceId}", message.InvoiceId);
            await _channel!.BasicNackAsync(ea.DeliveryTag, false, false, ct);
        }
    }

    public async Task HandleAsync(InvoiceJobMessage msg, CancellationToken ct)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IInvoiceRepository invoiceRepo = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        IInvoicePdfRenderer renderer = scope.ServiceProvider.GetRequiredService<IInvoicePdfRenderer>();
        IObjectStorageService storage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
        IEmailJobRepository emailJobRepo = scope.ServiceProvider.GetRequiredService<IEmailJobRepository>();

        Invoice? invoice = await invoiceRepo.GetByIdWithLinesAndRecipientsAsync(msg.InvoiceId, ct);
        if (invoice is null)
        {
            logger.LogWarning("Invoice {Id} not found, skipping", msg.InvoiceId);
            return;
        }

        try
        {
            byte[] pdfBytes = renderer.Render(invoice);
            string fileName = $"{invoice.Number}.pdf";
            string folder = $"invoices/{invoice.IssuedAt:yyyy}/{invoice.IssuedAt:MM}";
            string key = await storage.UploadAsync(pdfBytes, "application/pdf", folder, fileName, ct);

            invoice.StoragePath = key;
            invoice.Status = InvoiceStatus.Generated;
            invoice.FailureReason = null;
            await invoiceRepo.UpdateAsync(invoice, ct);

            InvoiceLegalSnapshotDto? recipient = JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(invoice.RecipientSnapshotJson);
            (string? emailAddress, string? recipientName, EmailJobType jobType) = ResolveEmailTarget(invoice, recipient);

            if (emailAddress is null)
            {
                logger.LogWarning(
                    "No email address found for invoice {Id} (Kind={Kind}), skipping email",
                    invoice.Id,
                    invoice.Kind
                );
                return;
            }

            object templateData = BuildTemplateData(invoice, jobType);
            EmailJob emailJob = new EmailJob
            {
                Type = jobType,
                Status = EmailJobStatus.Pending,
                RecipientEmail = emailAddress,
                RecipientName = recipientName,
                Subject = BuildSubject(invoice),
                TemplateData = JsonSerializer.Serialize(templateData),
                MaxRetries = 3,
                AttachmentStoragePath = key,
                AttachmentFilename = fileName,
            };

            await emailJobRepo.CreateAsync(emailJob, ct);
            await publisher.PublishAsync(MessagingExchanges.Email, new EmailJobMessage(emailJob.Id), ct);

            logger.LogInformation(
                "Invoice {Id} generated and email job {JobId} queued for {Email}",
                invoice.Id,
                emailJob.Id,
                emailAddress
            );
        }
        catch (Exception ex)
        {
            invoice.Status = InvoiceStatus.Failed;
            invoice.FailureReason = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            try
            {
                await invoiceRepo.UpdateAsync(invoice, ct);
            }
            catch
            {
                // best-effort
            }

            throw;
        }
    }

    private static (string? Email, string? Name, EmailJobType JobType) ResolveEmailTarget(
        Invoice invoice, InvoiceLegalSnapshotDto? recipient)
    {
        bool isCustomerKind =
            invoice.Kind == InvoiceKind.OrderInvoiceToCustomer
            || invoice.Kind == InvoiceKind.CreditNoteToCustomer;

        if (isCustomerKind)
        {
            // Prefer the dedicated Email field; fall back to Address for pre-existing snapshots,
            // then to the navigation-property email as a last resort.
            string? snapshotEmail = !string.IsNullOrWhiteSpace(recipient?.Email)
                ? recipient.Email
                : recipient?.Address;
            string? email = !string.IsNullOrWhiteSpace(snapshotEmail)
                ? snapshotEmail
                : invoice.RecipientUser?.Email;
            string? name = recipient?.Name;
            return (email, name, EmailJobType.InvoiceReadyCustomer);
        }
        else
        {
            // For restaurant invoices, use the owner's email from navigation property
            string? ownerEmail = invoice.RecipientRestaurant?.Owner?.Email;
            string? name = invoice.RecipientRestaurant?.Owner?.GetFullName() ?? recipient?.Name;
            return (ownerEmail, name, EmailJobType.InvoiceReadyRestaurant);
        }
    }

    private static object BuildTemplateData(Invoice invoice, EmailJobType jobType)
    {
        string totalTtc = invoice.TotalTtc.ToString("0.00");
        string issuedAt = invoice.IssuedAt.ToString("dd/MM/yyyy");
        string orderId = invoice.OrderId.ToString();

        if (jobType == EmailJobType.InvoiceReadyCustomer)
        {
            return new InvoiceReadyCustomerData(invoice.Number, orderId, issuedAt, totalTtc, invoice.Currency);
        }

        return new InvoiceReadyRestaurantData(invoice.Number, orderId, issuedAt, totalTtc, invoice.Currency);
    }

    private static string BuildSubject(Invoice invoice)
    {
        bool isCreditNote =
            invoice.Kind == InvoiceKind.CreditNoteToCustomer
            || invoice.Kind == InvoiceKind.CommissionCreditNoteToRestaurant;

        return isCreditNote
            ? $"Votre avoir {invoice.Number} est disponible"
            : $"Votre facture {invoice.Number} est disponible";
    }

    private async Task CleanupAsync()
    {
        try
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
                _channel = null;
            }

            if (_connection is not null)
            {
                await _connection.CloseAsync();
                _connection.Dispose();
                _connection = null;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error during RabbitMQ cleanup");
        }
    }
}
