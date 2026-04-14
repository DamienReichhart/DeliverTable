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
    public string[] CorsAllowedOrigins { get; }
    public int UploadMaxSizeMb { get; }
    public decimal PlatformCommissionRate { get; }
    public string RabbitMqHost { get; }
    public int RabbitMqPort { get; }
    public string RabbitMqUser { get; }
    public string RabbitMqPassword { get; }
    public string StripePublishableKey { get; }
    public string StripeSecretKey { get; }
    public string StripeWebhookSecret { get; }
    public string PlatformLegalName { get; }
    public string PlatformLegalForm { get; }
    public string PlatformSiret { get; }
    public string PlatformVatNumber { get; }
    public string PlatformAddress { get; }
    public bool PlatformVatApplicable { get; }

    private AppEnvironment(
        string databaseConnectionString,
        string redisConnectionString,
        JwtConfig jwt,
        ObjectStorageConfig objectStorage,
        bool openApiEnableDocumentation,
        string[] corsAllowedOrigins,
        int uploadMaxSizeMb,
        decimal platformCommissionRate,
        string rabbitMqHost,
        int rabbitMqPort,
        string rabbitMqUser,
        string rabbitMqPassword,
        string stripePublishableKey,
        string stripeSecretKey,
        string stripeWebhookSecret,
        string platformLegalName,
        string platformLegalForm,
        string platformSiret,
        string platformVatNumber,
        string platformAddress,
        bool platformVatApplicable)
    {
        DatabaseConnectionString = databaseConnectionString;
        RedisConnectionString = redisConnectionString;
        Jwt = jwt;
        ObjectStorage = objectStorage;
        OpenApiEnableDocumentation = openApiEnableDocumentation;
        CorsAllowedOrigins = corsAllowedOrigins;
        UploadMaxSizeMb = uploadMaxSizeMb;
        PlatformCommissionRate = platformCommissionRate;
        RabbitMqHost = rabbitMqHost;
        RabbitMqPort = rabbitMqPort;
        RabbitMqUser = rabbitMqUser;
        RabbitMqPassword = rabbitMqPassword;
        StripePublishableKey = stripePublishableKey;
        StripeSecretKey = stripeSecretKey;
        StripeWebhookSecret = stripeWebhookSecret;
        PlatformLegalName = platformLegalName;
        PlatformLegalForm = platformLegalForm;
        PlatformSiret = platformSiret;
        PlatformVatNumber = platformVatNumber;
        PlatformAddress = platformAddress;
        PlatformVatApplicable = platformVatApplicable;
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

        var corsOrigins = GetVar("CORS_ALLOWED_ORIGINS")
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];

        var uploadMaxSizeMb = ParseInt("UPLOAD_MAX_SIZE_MB",
            DeliverTableSharedLibrary.Constants.UploadLimits.DefaultMaxSizeMb, errors);

        var platformCommissionRate = ParseDecimal("PLATFORM_COMMISSION_RATE", 0.10m, errors);

        var rmqHost = RequireVar("RABBITMQ_HOST", errors);
        var rmqPort = ParseInt("RABBITMQ_PORT", 5672, errors);
        var rmqUser = RequireVar("RABBITMQ_USER", errors);
        var rmqPass = RequireVar("RABBITMQ_PASSWORD", errors);

        var stripePublishableKey = RequireVar("STRIPE_PUBLISHABLE_KEY", errors);
        var stripeSecretKey = RequireVar("STRIPE_SECRET_KEY", errors);
        var stripeWebhookSecret = RequireVar("STRIPE_WEBHOOK_SECRET", errors);

        var platformLegalName = RequireVar("PLATFORM_LEGAL_NAME", errors);
        var platformLegalForm = RequireVar("PLATFORM_LEGAL_FORM", errors);
        var platformSiret = RequireVar("PLATFORM_SIRET", errors);
        var platformVatNumber = RequireVar("PLATFORM_VAT_NUMBER", errors);
        var platformAddress = RequireVar("PLATFORM_ADDRESS", errors);
        var platformVatApplicable = ParseBool("PLATFORM_VAT_APPLICABLE", defaultValue: true);

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
            openApiEnable,
            corsOrigins,
            uploadMaxSizeMb,
            platformCommissionRate,
            rmqHost!,
            rmqPort,
            rmqUser!,
            rmqPass!,
            stripePublishableKey!,
            stripeSecretKey!,
            stripeWebhookSecret!,
            platformLegalName!,
            platformLegalForm!,
            platformSiret!,
            platformVatNumber!,
            platformAddress!,
            platformVatApplicable);
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

    private static decimal ParseDecimal(string name, decimal defaultValue, List<string> errors)
    {
        var raw = GetVar(name);
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;
        if (decimal.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        errors.Add($"{name} (expected decimal, got '{raw}')");
        return defaultValue;
    }
}
