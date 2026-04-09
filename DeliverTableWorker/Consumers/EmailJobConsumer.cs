using System.Text;
using System.Text.Json;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;
using DeliverTableWorker.Configuration;
using DeliverTableWorker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DeliverTableWorker.Consumers;

public class EmailJobConsumer(
    IServiceScopeFactory scopeFactory,
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    WorkerEnvironment env,
    ILogger<EmailJobConsumer> logger
) : BackgroundService
{
    private const string MainExchange = "delivertable.jobs";
    private const string DlxExchange = "delivertable.jobs.dlx";
    private const string DeadExchange = "delivertable.jobs.dead";
    private const string MainQueue = "delivertable.jobs.email";
    private const string DeadQueue = "delivertable.jobs.email.dead";
    private const string RoutingKey = "email";
    private const int MaxRetries = 3;

    private static readonly int[] RetryDelaysSeconds = [60, 300, 900];

    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTime _windowStart = DateTime.UtcNow;
    private int _sendsInWindow;

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
                    "Unhandled error processing message {DeliveryTag}",
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

        logger.LogInformation("EmailJobConsumer started, listening on {Queue}", MainQueue);

        // Keep alive until cancellation
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

        // Main exchange and queue
        await channel.ExchangeDeclareAsync(
            exchange: MainExchange,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: ct
        );

        var mainQueueArgs = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", DlxExchange },
        };

        await channel.QueueDeclareAsync(
            queue: MainQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: mainQueueArgs,
            cancellationToken: ct
        );

        await channel.QueueBindAsync(MainQueue, MainExchange, RoutingKey, cancellationToken: ct);

        // DLX exchange for retries
        await channel.ExchangeDeclareAsync(
            exchange: DlxExchange,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: ct
        );

        // Retry queues with escalating TTLs
        for (int i = 0; i < RetryDelaysSeconds.Length; i++)
        {
            var retryQueue = $"delivertable.jobs.email.retry.{i + 1}";
            var retryArgs = new Dictionary<string, object?>
            {
                { "x-message-ttl", RetryDelaysSeconds[i] * 1000 },
                { "x-dead-letter-exchange", MainExchange },
            };

            await channel.QueueDeclareAsync(
                queue: retryQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: retryArgs,
                cancellationToken: ct
            );

            await channel.QueueBindAsync(
                retryQueue,
                DlxExchange,
                $"email.retry.{i + 1}",
                cancellationToken: ct
            );
        }

        // Dead letter exchange and queue
        await channel.ExchangeDeclareAsync(
            exchange: DeadExchange,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: ct
        );

        await channel.QueueDeclareAsync(
            queue: DeadQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct
        );

        await channel.QueueBindAsync(
            DeadQueue,
            DeadExchange,
            RoutingKey,
            cancellationToken: ct
        );
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
        var message = JsonSerializer.Deserialize<EmailJobMessage>(json);

        if (message is null)
        {
            logger.LogWarning("Received null or invalid message, acking and skipping");
            await _channel!.BasicAckAsync(ea.DeliveryTag, false, ct);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEmailJobRepository>();
        var job = await repo.GetByIdAsync(message.JobId, ct);

        // Idempotency check
        if (
            job is null
            || job.Status == EmailJobStatus.Sent
            || job.Status == EmailJobStatus.DeadLettered
        )
        {
            if (job is null)
                logger.LogWarning("EmailJob {JobId} not found, skipping", message.JobId);
            else
                logger.LogInformation(
                    "EmailJob {JobId} already in terminal status {Status}, skipping",
                    message.JobId,
                    job.Status
                );

            await _channel!.BasicAckAsync(ea.DeliveryTag, false, ct);
            return;
        }

        // Neutralize mode: skip sending, mark as sent immediately
        if (env.NeutralizeEmail)
        {
            job.Status = EmailJobStatus.Sent;
            job.ProcessedAt = DateTime.UtcNow;
            job.CompletedAt = DateTime.UtcNow;
            await repo.UpdateAsync(job, ct);
            await _channel!.BasicAckAsync(ea.DeliveryTag, false, ct);

            logger.LogInformation(
                "EmailJob {JobId} neutralized (NEUTRALIZE_EMAIL=true) — skipped sending to {Recipient}, subject: {Subject}",
                job.Id,
                job.RecipientEmail,
                job.Subject);
            return;
        }

        // Mark as processing
        job.Status = EmailJobStatus.Processing;
        job.ProcessedAt = DateTime.UtcNow;
        await repo.UpdateAsync(job, ct);

        try
        {
            // Rate limiting
            await WaitForRateLimitAsync(ct);

            // Render template
            var htmlBody = await templateRenderer.RenderAsync(job.Type, job.TemplateData, ct);

            // Send email
            await emailSender.SendAsync(job.RecipientEmail, job.RecipientName, job.Subject, htmlBody, ct);

            // Success
            job.Status = EmailJobStatus.Sent;
            job.CompletedAt = DateTime.UtcNow;
            await repo.UpdateAsync(job, ct);
            await _channel!.BasicAckAsync(ea.DeliveryTag, false, ct);

            logger.LogInformation(
                "EmailJob {JobId} sent successfully to {Recipient}",
                job.Id,
                job.RecipientEmail
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process EmailJob {JobId}", job.Id);

            job.RetryCount++;
            job.ErrorMessage = ex.Message.Length > 2000
                ? ex.Message[..2000]
                : ex.Message;

            if (job.RetryCount < MaxRetries)
            {
                job.Status = EmailJobStatus.RetryScheduled;
                await repo.UpdateAsync(job, ct);

                // Publish to retry queue via DLX exchange
                var retryRoutingKey = $"email.retry.{job.RetryCount}";
                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                };
                var body = Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(new EmailJobMessage(job.Id))
                );

                await _channel!.BasicPublishAsync(
                    exchange: DlxExchange,
                    routingKey: retryRoutingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: ct
                );

                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);

                logger.LogWarning(
                    "EmailJob {JobId} scheduled for retry {Retry}/{Max} via {Queue}",
                    job.Id,
                    job.RetryCount,
                    MaxRetries,
                    $"delivertable.jobs.email.retry.{job.RetryCount}"
                );
            }
            else
            {
                job.Status = EmailJobStatus.DeadLettered;
                job.CompletedAt = DateTime.UtcNow;
                await repo.UpdateAsync(job, ct);
                await _channel!.BasicAckAsync(ea.DeliveryTag, false, ct);

                logger.LogError(
                    "EmailJob {JobId} dead-lettered after {Retries} retries. Error: {Error}",
                    job.Id,
                    job.RetryCount,
                    job.ErrorMessage
                );
            }
        }
    }

    private async Task WaitForRateLimitAsync(CancellationToken ct)
    {
        await _rateLimiter.WaitAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            if ((now - _windowStart).TotalSeconds >= 60)
            {
                _windowStart = now;
                _sendsInWindow = 0;
            }

            if (_sendsInWindow >= env.SmtpMaxSendsPerMinute)
            {
                var waitTime = _windowStart.AddSeconds(60) - now;
                if (waitTime > TimeSpan.Zero)
                {
                    logger.LogInformation(
                        "Rate limit reached ({Max}/min), waiting {Seconds}s",
                        env.SmtpMaxSendsPerMinute,
                        waitTime.TotalSeconds
                    );
                    await Task.Delay(waitTime, ct);
                }

                _windowStart = DateTime.UtcNow;
                _sendsInWindow = 0;
            }

            _sendsInWindow++;
        }
        finally
        {
            _rateLimiter.Release();
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
