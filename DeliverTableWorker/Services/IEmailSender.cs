namespace DeliverTableWorker.Services;

public interface IEmailSender
{
    Task SendAsync(
        string to,
        string? toName,
        string subject,
        string htmlBody,
        CancellationToken ct = default
    );
}
