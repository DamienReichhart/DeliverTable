using System.Text.Json.Serialization;

namespace DeliverTableClient.Configuration;

/// <summary>
///     JSON-serializable shape of appconfig.json. Used for deserialization only.
/// </summary>
internal sealed class AppConfigurationOptions
{
    public const string ConfigFileName = "appconfig.json";
    public const string DevelopmentConfigFileName = "appconfig.Development.json";

    [JsonPropertyName("api")] public ApiSection? Api { get; set; }

    [JsonPropertyName("environment")] public string? Environment { get; set; }

    internal sealed class ApiSection
    {
        [JsonPropertyName("baseUrl")] public string? BaseUrl { get; set; }
    }
}