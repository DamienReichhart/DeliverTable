namespace DeliverTableWorker.Configuration;

public sealed class WorkerEnvironment
{
    public string ConnectionStringDatabase { get; }
    public string RabbitMqHost { get; }
    public int RabbitMqPort { get; }
    public string RabbitMqUser { get; }
    public string RabbitMqPassword { get; }
    public string SmtpHost { get; }
    public int SmtpPort { get; }
    public string SmtpUser { get; }
    public string SmtpPassword { get; }
    public string SmtpFromEmail { get; }
    public string SmtpFromName { get; }
    public int SmtpMaxSendsPerMinute { get; }
    public bool NeutralizeEmail { get; }
    public string PlatformLegalName { get; }
    public string PlatformLegalForm { get; }
    public string PlatformSiret { get; }
    public string PlatformVatNumber { get; }
    public string PlatformAddress { get; }
    public bool PlatformVatApplicable { get; }

    private WorkerEnvironment(
        string connectionStringDatabase,
        string rabbitMqHost, int rabbitMqPort, string rabbitMqUser, string rabbitMqPassword,
        string smtpHost, int smtpPort, string smtpUser, string smtpPassword,
        string smtpFromEmail, string smtpFromName, int smtpMaxSendsPerMinute,
        bool neutralizeEmail,
        string platformLegalName, string platformLegalForm, string platformSiret,
        string platformVatNumber, string platformAddress, bool platformVatApplicable)
    {
        ConnectionStringDatabase = connectionStringDatabase;
        RabbitMqHost = rabbitMqHost;
        RabbitMqPort = rabbitMqPort;
        RabbitMqUser = rabbitMqUser;
        RabbitMqPassword = rabbitMqPassword;
        SmtpHost = smtpHost;
        SmtpPort = smtpPort;
        SmtpUser = smtpUser;
        SmtpPassword = smtpPassword;
        SmtpFromEmail = smtpFromEmail;
        SmtpFromName = smtpFromName;
        SmtpMaxSendsPerMinute = smtpMaxSendsPerMinute;
        NeutralizeEmail = neutralizeEmail;
        PlatformLegalName = platformLegalName;
        PlatformLegalForm = platformLegalForm;
        PlatformSiret = platformSiret;
        PlatformVatNumber = platformVatNumber;
        PlatformAddress = platformAddress;
        PlatformVatApplicable = platformVatApplicable;
    }

    public static WorkerEnvironment Load()
    {
        var errors = new List<string>();

        var dbConn = RequireVar("CONNECTION_STRING_DATABASE", errors);
        var rmqHost = RequireVar("RABBITMQ_HOST", errors);
        var rmqPort = ParseInt("RABBITMQ_PORT", 5672, errors);
        var rmqUser = RequireVar("RABBITMQ_USER", errors);
        var rmqPass = RequireVar("RABBITMQ_PASSWORD", errors);
        var smtpHost = RequireVar("SMTP_HOST", errors);
        var smtpPort = ParseInt("SMTP_PORT", 465, errors);
        var smtpUser = RequireVar("SMTP_USER", errors);
        var smtpPass = RequireVar("SMTP_PASSWORD", errors);
        var smtpFrom = RequireVar("SMTP_FROM_EMAIL", errors);
        var smtpName = GetVar("SMTP_FROM_NAME") ?? "DeliverTable";
        var smtpRate = ParseInt("SMTP_MAX_SENDS_PER_MINUTE", 5, errors);
        var neutralizeEmail = ParseBool("NEUTRALIZE_EMAIL");

        var platformLegalName = RequireVar("PLATFORM_LEGAL_NAME", errors);
        var platformLegalForm = RequireVar("PLATFORM_LEGAL_FORM", errors);
        var platformSiret = RequireVar("PLATFORM_SIRET", errors);
        var platformVatNumber = RequireVar("PLATFORM_VAT_NUMBER", errors);
        var platformAddress = RequireVar("PLATFORM_ADDRESS", errors);
        var platformVatApplicable = ParseBool("PLATFORM_VAT_APPLICABLE");

        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"Missing or invalid environment variables:\n- {string.Join("\n- ", errors)}");

        return new WorkerEnvironment(
            dbConn!, rmqHost!, rmqPort, rmqUser!, rmqPass!,
            smtpHost!, smtpPort, smtpUser!, smtpPass!,
            smtpFrom!, smtpName, smtpRate,
            neutralizeEmail,
            platformLegalName!, platformLegalForm!, platformSiret!,
            platformVatNumber!, platformAddress!, platformVatApplicable);
    }

    private static string? GetVar(string name) => Environment.GetEnvironmentVariable(name);

    private static string? RequireVar(string name, List<string> errors)
    {
        var value = GetVar(name);
        if (string.IsNullOrWhiteSpace(value)) errors.Add(name);
        return value;
    }

    private static bool ParseBool(string name)
    {
        var raw = GetVar(name);
        return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static int ParseInt(string name, int defaultValue, List<string> errors)
    {
        var raw = GetVar(name);
        if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
        if (int.TryParse(raw, out var result)) return result;
        errors.Add($"{name} (expected integer, got '{raw}')");
        return defaultValue;
    }
}
