using Microsoft.AspNetCore.OpenApi;

namespace DeliverTableServer.Configuration;

/// <summary>
/// Extension methods for registering and configuring OpenAPI services with professional document metadata.
/// </summary>
public static class OpenApiServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenAPI document generation with DeliverTable-specific metadata (title, description, contact, license, servers).
    /// Uses the built-in Microsoft.AspNetCore.OpenApi pipeline and a document transformer for full control.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDeliverTableOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(OpenApiConstants.DocumentName, options =>
        {
            options.AddDocumentTransformer(new OpenApiDocumentTransformer());
        });

        return services;
    }
}
