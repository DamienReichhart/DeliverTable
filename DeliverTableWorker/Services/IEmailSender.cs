namespace DeliverTableWorker.Services;

public interface IEmailSender
{
    Task SendAsync(
        string to,
        string? toName,
        string subject,
        string htmlBody,
        AttachmentPayload? attachment = null,
        CancellationToken ct = default
    );
}

/// <summary>
/// Carries the in-memory bytes and metadata for a single email attachment.
/// </summary>
public sealed record AttachmentPayload(byte[] Bytes, string Filename);
