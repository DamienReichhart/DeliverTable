using System.Text.Json;
using DeliverTableInfrastructure.TemplateData;
using DeliverTableSharedLibrary.Enums;
using RazorLight;

namespace DeliverTableWorker.Services;

public class RazorEmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly RazorLightEngine _engine;

    public RazorEmailTemplateRenderer()
    {
        var templatesPath = Path.Combine(AppContext.BaseDirectory, "Templates", "Email");
        _engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(templatesPath)
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<string> RenderAsync(EmailJobType type, string templateDataJson, CancellationToken ct = default)
    {
        return type switch
        {
            EmailJobType.OrderConfirmation => await RenderTypedAsync<OrderConfirmationData>("OrderConfirmation", templateDataJson),
            EmailJobType.OrderStatusUpdate => await RenderTypedAsync<OrderStatusUpdateData>("OrderStatusUpdate", templateDataJson),
            EmailJobType.OrderDelivered => await RenderTypedAsync<OrderDeliveredData>("OrderDelivered", templateDataJson),
            EmailJobType.OrderCancelled => await RenderTypedAsync<OrderCancelledData>("OrderCancelled", templateDataJson),
            EmailJobType.OrderReady => await RenderTypedAsync<OrderReadyData>("OrderReady", templateDataJson),
            EmailJobType.NewOrderForRestaurant => await RenderTypedAsync<NewOrderForRestaurantData>("NewOrderForRestaurant", templateDataJson),
            EmailJobType.PasswordReset => await RenderTypedAsync<PasswordResetData>("PasswordReset", templateDataJson),
            EmailJobType.PasswordChanged => await RenderTypedAsync<PasswordChangedData>("PasswordChanged", templateDataJson),
            EmailJobType.WelcomeEmail => await RenderTypedAsync<WelcomeEmailData>("WelcomeEmail", templateDataJson),
            EmailJobType.InvoiceReadyCustomer => await RenderTypedAsync<InvoiceReadyCustomerData>("InvoiceReadyCustomer", templateDataJson),
            EmailJobType.InvoiceReadyRestaurant => await RenderTypedAsync<InvoiceReadyRestaurantData>("InvoiceReadyRestaurant", templateDataJson),
            EmailJobType.DisputeOpenedAdmin => await RenderTypedAsync<DisputeEmailData>("DisputeOpenedAdmin", templateDataJson),
            EmailJobType.DisputeOpenedRestaurant => await RenderTypedAsync<DisputeEmailData>("DisputeOpenedRestaurant", templateDataJson),
            EmailJobType.DisputeWonAdmin => await RenderTypedAsync<DisputeEmailData>("DisputeWonAdmin", templateDataJson),
            EmailJobType.DisputeWonRestaurant => await RenderTypedAsync<DisputeEmailData>("DisputeWonRestaurant", templateDataJson),
            EmailJobType.DisputeLostAdmin => await RenderTypedAsync<DisputeEmailData>("DisputeLostAdmin", templateDataJson),
            EmailJobType.DisputeLostRestaurant => await RenderTypedAsync<DisputeEmailData>("DisputeLostRestaurant", templateDataJson),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown email job type")
        };
    }

    private async Task<string> RenderTypedAsync<T>(string templateName, string json)
    {
        var data = JsonSerializer.Deserialize<T>(json)
            ?? throw new InvalidOperationException($"Failed to deserialize template data for {templateName}");
        return await _engine.CompileRenderAsync($"{templateName}.cshtml", data);
    }
}
