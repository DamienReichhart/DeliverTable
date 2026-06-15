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
        AttachmentPayload? attachment = null,
        CancellationToken ct = default
    )
    {
        MimeMessage message = new MimeMessage();
        message.From.Add(new MailboxAddress(env.SmtpFromName, env.SmtpFromEmail));
        message.To.Add(new MailboxAddress(toName ?? to, to));
        message.Subject = subject;

        TextPart htmlPart = new TextPart("html") { Text = htmlBody };

        if (attachment is not null)
        {
            Multipart multipart = new Multipart("mixed");
            multipart.Add(htmlPart);
            multipart.Add(new MimePart("application", "pdf")
            {
                Content = new MimeContent(new MemoryStream(attachment.Bytes)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment) { FileName = attachment.Filename },
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = attachment.Filename,
            });
            message.Body = multipart;
        }
        else
        {
            message.Body = htmlPart;
        }

        using SmtpClient client = new SmtpClient();
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
