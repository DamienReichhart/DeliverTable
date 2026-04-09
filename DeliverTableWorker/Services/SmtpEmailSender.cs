using DeliverTableWorker.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DeliverTableWorker.Services;

public class SmtpEmailSender(WorkerEnvironment env, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(
        string to,
        string? toName,
        string subject,
        string htmlBody,
        CancellationToken ct = default
    )
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(env.SmtpFromName, env.SmtpFromEmail));
        message.To.Add(new MailboxAddress(toName ?? to, to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(env.SmtpHost, env.SmtpPort, SecureSocketOptions.SslOnConnect, ct);
        await client.AuthenticateAsync(env.SmtpUser, env.SmtpPassword, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        logger.LogInformation(
            "Email sent to {Recipient} — subject: {Subject}",
            to,
            subject
        );
    }
}
