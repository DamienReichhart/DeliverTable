using DeliverTableSharedLibrary.Enums;

namespace DeliverTableWorker.Services;

public interface IEmailTemplateRenderer
{
    Task<string> RenderAsync(EmailJobType type, string templateDataJson, CancellationToken ct = default);
}
