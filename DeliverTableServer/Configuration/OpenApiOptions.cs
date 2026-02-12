namespace DeliverTableServer.Configuration;

/// <summary>
/// Options for OpenAPI/Swagger documentation. Bound from configuration (appsettings or environment).
/// </summary>
public sealed class OpenApiOptions
{
    /// <summary>
    /// Configuration key for the OpenApi section.
    /// </summary>
    public const string SectionName = "OpenApi";

    /// <summary>
    /// When true, exposes the OpenAPI document and Swagger UI even outside Development.
    /// When false, documentation is only enabled in Development (recommended for production).
    /// </summary>
    public bool EnableDocumentation { get; set; }
}
