using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace DeliverTableServer.Configuration;

/// <summary>
///     Enriches the generated OpenAPI document with professional metadata:
///     title, description, version, contact, license, terms of service, and servers.
///     Applied via AddDocumentTransformer so the spec is suitable for public documentation and tooling.
/// </summary>
internal sealed class OpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    private const string ApiTitle = "DeliverTable API";
    private const string ApiVersion = "1.0.0";

    private const string ApiDescription =
        "REST API for the DeliverTable service. " +
        "Use this documentation to discover endpoints, request/response schemas, and try out operations.";

    /// <inheritdoc />
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.Info = new OpenApiInfo
        {
            Title = ApiTitle,
            Version = ApiVersion,
            Description = ApiDescription,
            Contact = new OpenApiContact
            {
                Name = "DeliverTable Team",
                Email = "support@delivertable.example",
                Url = new Uri("https://delivertable.example/support", UriKind.Absolute)
            },
            License = new OpenApiLicense
            {
                Name = "Proprietary",
                Url = new Uri("https://delivertable.example/terms", UriKind.Absolute)
            },
            TermsOfService = new Uri("https://delivertable.example/terms", UriKind.Absolute)
        };

        // Single server entry; can be overridden by hosting or configuration if needed.
        document.Servers =
        [
            new OpenApiServer
            {
                Url = "/",
                Description = "Relative to the current host (development or deployed base URL)."
            }
        ];

        return Task.CompletedTask;
    }
}