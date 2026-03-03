namespace DeliverTableServer.Configuration;

/// <summary>
///     Centralized, immutable snapshot of every server environment variable.
///     Loaded once at startup; validates all required variables and fails fast with a single
///     aggregated error message listing every missing or malformed entry.
/// </summary>
public sealed class AppEnvironment
{
    public string DatabaseConnectionString { get; }
    public string RedisConnectionString { get; }
    public JwtConfig Jwt { get; }
    public ObjectStorageConfig ObjectStorage { get; }
    public bool OpenApiEnableDocumentation { get; }

    private AppEnvironment(
        string databaseConnectionString,
        string redisConnectionString,
        JwtConfig jwt,
        ObjectStorageConfig objectStorage,
        bool openApiEnableDocumentation)
    {
        DatabaseConnectionString = databaseConnectionString;
        RedisConnectionString = redisConnectionString;
        Jwt = jwt;
        ObjectStorage = objectStorage;
        OpenApiEnableDocumentation = openApiEnableDocumentation;
    }

    /// <summary>
    ///     Reads all server environment variables, validates required ones, and returns a frozen configuration.
    ///     Throws <see cref="InvalidOperationException" /> listing every missing or invalid variable.
    /// </summary>
    public static AppEnvironment Load()
    {
        var errors = new List<string>();

        var dbConn = RequireVar("CONNECTION_STRING_DATABASE", errors);
        var redisConn = GetVar("CONNECTION_STRING_REDIS") ?? "";

        var jwtKey = RequireVar("JWT_KEY", errors);
        var jwtIssuer = RequireVar("JWT_ISSUER", errors);
        var jwtAudience = RequireVar("JWT_AUDIENCE", errors);
        var jwtExpire = ParseInt("JWT_EXPIRE_MINUTES", 60, errors);

        var osUrl = RequireVar("OBJECT_STORAGE_SERVICE_URL", errors);
        var osAccessKey = RequireVar("OBJECT_STORAGE_ACCESS_KEY", errors);
        var osSecretKey = RequireVar("OBJECT_STORAGE_SECRET_KEY", errors);
        var osBucket = RequireVar("OBJECT_STORAGE_BUCKET_NAME", errors);
        var osForcePathStyle = ParseBool("OBJECT_STORAGE_FORCE_PATH_STYLE", defaultValue: true);
        var osRegion = GetVar("OBJECT_STORAGE_REGION") ?? "garage";

        var openApiEnable = ParseBool("OPENAPI_ENABLE_DOCUMENTATION", defaultValue: false);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing or invalid environment variables:\n- {string.Join("\n- ", errors)}");
        }

        return new AppEnvironment(
            dbConn!,
            redisConn,
            new JwtConfig
            {
                Key = jwtKey!,
                Issuer = jwtIssuer!,
                Audience = jwtAudience!,
                ExpireMinutes = jwtExpire
            },
            new ObjectStorageConfig(osUrl!, osAccessKey!, osSecretKey!, osBucket!, osForcePathStyle, osRegion),
            openApiEnable);
    }

    private static string? GetVar(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

    private static string? RequireVar(string name, List<string> errors)
    {
        var value = GetVar(name);
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(name);
        return value;
    }

    private static int ParseInt(string name, int defaultValue, List<string> errors)
    {
        var raw = GetVar(name);
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;
        if (int.TryParse(raw, out var result))
            return result;
        errors.Add($"{name} (expected integer, got '{raw}')");
        return defaultValue;
    }

    private static bool ParseBool(string name, bool defaultValue)
    {
        var raw = GetVar(name);
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;
        return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
    }
}
