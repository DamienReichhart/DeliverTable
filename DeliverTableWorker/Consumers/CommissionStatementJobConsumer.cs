using System.Text;
using System.Text.Json;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableInfrastructure.Services.Interfaces;
using DeliverTableInfrastructure.TemplateData;
using DeliverTableSharedLibrary.Enums;
using DeliverTableWorker.Configuration;
using DeliverTableWorker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DeliverTableWorker.Consumers;

public sealed class CommissionStatementJobConsumer(
    IServiceScopeFactory scopeFactory,
    IMessagePublisher publisher,
    WorkerEnvironment env,
    ILogger<CommissionStatementJobConsumer> logger) : BackgroundService
{
    private const string MainExchange = "delivertable.jobs";
    private const string MainQueue = "delivertable.jobs.commission_statement";
    private const string RoutingKey = MessagingExchanges.CommissionStatement;

    private static readonly string[] MoisFrancaisNames =
    [
        "janvier",
        "février",
        "mars",
        "avril",
        "mai",
        "juin",
        "juillet",
        "août",
        "septembre",
        "octobre",
        "novembre",
        "décembre",
    ];

    private static string MoisFrancais(int month) => MoisFrancaisNames[month - 1];

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
        var factory = new ConnectionFactory
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

        var consumer = new AsyncEventingBasicConsumer(_channel);
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
                    "Unhandled error processing commission statement message {DeliveryTag}",
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

        logger.LogInformation(
            "CommissionStatementJobConsumer started, listening on {Queue}",
            MainQueue
        );

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
        var channel = _channel ?? throw new InvalidOperationException("Channel not initialized");

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
        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
        var message = JsonSerializer.Deserialize<CommissionStatementJobMessage>(json);

        if (message is null)
        {
            logger.LogWarning(
                "Received null or invalid commission statement message, acking and skipping"
            );
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
            logger.LogError(
                ex,
                "Failed to process commission statement job for Id {Id}",
                message.CommissionStatementId
            );
            await _channel!.BasicNackAsync(ea.DeliveryTag, false, false, ct);
        }
    }

    public async Task HandleAsync(CommissionStatementJobMessage msg, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var statementRepo = scope.ServiceProvider.GetRequiredService<ICommissionStatementRepository>();
        var renderer = scope.ServiceProvider.GetRequiredService<ICommissionStatementPdfRenderer>();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
        var emailJobRepo = scope.ServiceProvider.GetRequiredService<IEmailJobRepository>();

        var statement = await statementRepo.GetByIdWithLinesAndRecipientAsync(
            msg.CommissionStatementId,
            ct
        );
        if (statement is null)
        {
            logger.LogWarning(
                "CommissionStatement {Id} not found, skipping",
                msg.CommissionStatementId
            );
            return;
        }

        try
        {
            var pdfBytes = renderer.Render(statement);
            string fileName = $"{statement.Number}.pdf";
            string folder =
                $"commission-statements/{statement.PeriodYear}/{statement.PeriodMonth:D2}";
            var key = await storage.UploadAsync(pdfBytes, "application/pdf", folder, fileName, ct);

            statement.StoragePath = key;
            statement.Status = CommissionStatementStatus.Generated;
            statement.FailureReason = null;
            await statementRepo.UpdateAsync(statement, ct);

            var recipientEmail =
                statement.RecipientEmailSnapshot
                ?? statement.RecipientRestaurant?.Owner?.Email;

            if (recipientEmail is null)
            {
                logger.LogWarning(
                    "No email address found for commission statement {Id} (Kind={Kind}), skipping email",
                    statement.Id,
                    statement.Kind
                );
                return;
            }

            var recipientName = statement.RecipientRestaurant?.Name ?? "";
            var (subject, jobType, templateData) = BuildEmailJob(statement);

            var emailJob = new EmailJob
            {
                Type = jobType,
                Status = EmailJobStatus.Pending,
                RecipientEmail = recipientEmail,
                RecipientName = recipientName,
                Subject = subject,
                TemplateData = JsonSerializer.Serialize(templateData),
                MaxRetries = 3,
                AttachmentStoragePath = key,
                AttachmentFilename = fileName,
            };

            await emailJobRepo.CreateAsync(emailJob, ct);
            await publisher.PublishAsync(
                MessagingExchanges.Email,
                new EmailJobMessage(emailJob.Id),
                ct
            );

            logger.LogInformation(
                "CommissionStatement {Id} generated and email job {JobId} queued for {Email}",
                statement.Id,
                emailJob.Id,
                recipientEmail
            );
        }
        catch (Exception ex)
        {
            statement.Status = CommissionStatementStatus.Failed;
            statement.FailureReason = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            try
            {
                await statementRepo.UpdateAsync(statement, ct);
            }
            catch
            {
                // best-effort
            }

            throw;
        }
    }

    private static (string Subject, EmailJobType JobType, object TemplateData) BuildEmailJob(
        CommissionStatement statement
    )
    {
        var issuedAt = statement.IssuedAt.ToString("dd/MM/yyyy");
        var totalTtc = statement.TotalTtc.ToString("0.00");
        var mois = MoisFrancais(statement.PeriodMonth);

        if (statement.Kind == CommissionStatementKind.Invoice)
        {
            var periodLabel = $"{mois} {statement.PeriodYear}";
            var subject =
                $"Votre relevé de commissions de {mois} {statement.PeriodYear} est disponible";
            var data = new CommissionStatementInvoiceData(
                statement.Number,
                periodLabel,
                issuedAt,
                totalTtc,
                statement.Currency
            );
            return (subject, EmailJobType.CommissionStatementInvoice, data);
        }
        else
        {
            var firstLine = statement.Lines.FirstOrDefault();
            var orderNumber = firstLine?.OrderNumber ?? "";
            var subject = $"Avoir sur commissions — commande {orderNumber}";
            var data = new CommissionStatementCreditNoteData(
                statement.Number,
                orderNumber,
                issuedAt,
                totalTtc,
                statement.Currency
            );
            return (subject, EmailJobType.CommissionStatementCreditNote, data);
        }
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
