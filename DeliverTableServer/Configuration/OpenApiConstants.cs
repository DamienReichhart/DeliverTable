namespace DeliverTableServer.Configuration;

/// <summary>
/// Constants for OpenAPI/Swagger documentation routes and document names.
/// Centralizes paths so documentation is consistently exposed under /api/v1/documentation.
/// </summary>
public static class OpenApiConstants
{
    /// <summary>
    /// Base path for all documentation endpoints (Swagger UI and OpenAPI JSON).
    /// </summary>
    public const string DocumentationPath = "api/v1/documentation";

    /// <summary>
    /// Route template for the OpenAPI JSON document endpoint.
    /// The {documentName} parameter is required by the framework (e.g. "v1").
    /// </summary>
    public const string OpenApiRouteTemplate = DocumentationPath + "/{documentName}.json";

    /// <summary>
    /// Default OpenAPI document name registered with AddOpenApi.
    /// </summary>
    public const string DocumentName = "v1";

    /// <summary>
    /// Full URL path to the default OpenAPI JSON document (for Swagger UI endpoint configuration).
    /// </summary>
    public static string OpenApiJsonPath => $"/{DocumentationPath}/{DocumentName}.json";
}
