using DeliverTableServer.Configuration;

namespace DeliverTableTests.Server.Unit.Configuration;

[TestFixture]
public class AppEnvironmentTests
{
    private static readonly string[] AllRequiredVars =
    [
        "CONNECTION_STRING_DATABASE",
        "JWT_KEY", "JWT_ISSUER", "JWT_AUDIENCE",
        "OBJECT_STORAGE_SERVICE_URL", "OBJECT_STORAGE_ACCESS_KEY",
        "OBJECT_STORAGE_SECRET_KEY", "OBJECT_STORAGE_BUCKET_NAME",
        "RABBITMQ_HOST", "RABBITMQ_USER", "RABBITMQ_PASSWORD",
        "STRIPE_PUBLISHABLE_KEY", "STRIPE_SECRET_KEY", "STRIPE_WEBHOOK_SECRET",
        "PLATFORM_LEGAL_NAME", "PLATFORM_LEGAL_FORM", "PLATFORM_SIRET",
        "PLATFORM_VAT_NUMBER", "PLATFORM_ADDRESS",
        "ADMIN_DISPUTE_EMAIL"
    ];

    private static readonly string[] AllOptionalVars =
    [
        "CONNECTION_STRING_REDIS",
        "JWT_EXPIRE_MINUTES",
        "OBJECT_STORAGE_FORCE_PATH_STYLE",
        "OPENAPI_ENABLE_DOCUMENTATION",
        "CORS_ALLOWED_ORIGINS",
        "UPLOAD_MAX_SIZE_MB",
        "RABBITMQ_PORT",
        "PLATFORM_VAT_APPLICABLE"
    ];

    [TearDown]
    public void TearDown()
    {
        foreach (string name in AllRequiredVars)
            Environment.SetEnvironmentVariable(name, null);
        foreach (string name in AllOptionalVars)
            Environment.SetEnvironmentVariable(name, null);
    }

    [Test]
    public void Load_ReadsAllRequiredVariables()
    {
        SetAllRequired();

        AppEnvironment env = AppEnvironment.Load();

        Assert.Multiple(() =>
        {
            Assert.That(env.DatabaseConnectionString, Is.EqualTo("Host=localhost;Database=test"));
            Assert.That(env.Jwt.Key, Is.EqualTo("test-key"));
            Assert.That(env.Jwt.Issuer, Is.EqualTo("test-issuer"));
            Assert.That(env.Jwt.Audience, Is.EqualTo("test-audience"));
            Assert.That(env.ObjectStorage.ServiceUrl, Is.EqualTo("http://localhost:3900"));
            Assert.That(env.ObjectStorage.AccessKey, Is.EqualTo("ak"));
            Assert.That(env.ObjectStorage.SecretKey, Is.EqualTo("sk"));
            Assert.That(env.ObjectStorage.BucketName, Is.EqualTo("bucket"));
            Assert.That(env.RabbitMqHost, Is.EqualTo("localhost"));
            Assert.That(env.RabbitMqUser, Is.EqualTo("guest"));
            Assert.That(env.RabbitMqPassword, Is.EqualTo("guest"));
            Assert.That(env.StripePublishableKey, Is.EqualTo("pk_test_stripe"));
            Assert.That(env.StripeSecretKey, Is.EqualTo("sk_test_stripe"));
            Assert.That(env.StripeWebhookSecret, Is.EqualTo("whsec_test_stripe"));
            Assert.That(env.PlatformLegalName, Is.EqualTo("Test Platform"));
            Assert.That(env.PlatformLegalForm, Is.EqualTo("SAS"));
            Assert.That(env.PlatformSiret, Is.EqualTo("73282932000074"));
            Assert.That(env.PlatformVatNumber, Is.EqualTo("FR12345678900"));
            Assert.That(env.PlatformAddress, Is.EqualTo("1 rue Test, 75001 Paris"));
            Assert.That(env.PlatformVatApplicable, Is.True);
            Assert.That(env.AdminDisputeEmail, Is.EqualTo("disputes@test.local"));
        });
    }

    [Test]
    public void Load_AppliesDefaults_WhenOptionalVarsAreMissing()
    {
        SetAllRequired();

        AppEnvironment env = AppEnvironment.Load();

        Assert.Multiple(() =>
        {
            Assert.That(env.RedisConnectionString, Is.EqualTo(string.Empty));
            Assert.That(env.Jwt.ExpireMinutes, Is.EqualTo(60));
            Assert.That(env.ObjectStorage.ForcePathStyle, Is.True);
            Assert.That(env.OpenApiEnableDocumentation, Is.False);
            Assert.That(env.CorsAllowedOrigins, Is.Empty);
            Assert.That(env.UploadMaxSizeMb, Is.EqualTo(5));
            Assert.That(env.RabbitMqPort, Is.EqualTo(5672));
        });
    }

    [Test]
    public void Load_ReadsOptionalVariables_WhenSet()
    {
        SetAllRequired();
        Environment.SetEnvironmentVariable("CONNECTION_STRING_REDIS", "localhost:6379");
        Environment.SetEnvironmentVariable("JWT_EXPIRE_MINUTES", "120");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_FORCE_PATH_STYLE", "false");
        Environment.SetEnvironmentVariable("OPENAPI_ENABLE_DOCUMENTATION", "true");
        Environment.SetEnvironmentVariable("UPLOAD_MAX_SIZE_MB", "10");

        AppEnvironment env = AppEnvironment.Load();

        Assert.Multiple(() =>
        {
            Assert.That(env.RedisConnectionString, Is.EqualTo("localhost:6379"));
            Assert.That(env.Jwt.ExpireMinutes, Is.EqualTo(120));
            Assert.That(env.ObjectStorage.ForcePathStyle, Is.False);
            Assert.That(env.OpenApiEnableDocumentation, Is.True);
            Assert.That(env.UploadMaxSizeMb, Is.EqualTo(10));
        });
    }

    [Test]
    public void Load_ThrowsWithAllMissingVars_WhenNoneAreSet()
    {
        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() => AppEnvironment.Load());

        Assert.Multiple(() =>
        {
            foreach (string name in AllRequiredVars)
                Assert.That(ex!.Message, Does.Contain(name));
        });
    }

    [TestCase("CONNECTION_STRING_DATABASE")]
    [TestCase("JWT_KEY")]
    [TestCase("JWT_ISSUER")]
    [TestCase("JWT_AUDIENCE")]
    [TestCase("OBJECT_STORAGE_SERVICE_URL")]
    [TestCase("OBJECT_STORAGE_ACCESS_KEY")]
    [TestCase("OBJECT_STORAGE_SECRET_KEY")]
    [TestCase("OBJECT_STORAGE_BUCKET_NAME")]
    [TestCase("RABBITMQ_HOST")]
    [TestCase("RABBITMQ_USER")]
    [TestCase("RABBITMQ_PASSWORD")]
    [TestCase("STRIPE_PUBLISHABLE_KEY")]
    [TestCase("STRIPE_SECRET_KEY")]
    [TestCase("STRIPE_WEBHOOK_SECRET")]
    [TestCase("PLATFORM_LEGAL_NAME")]
    [TestCase("PLATFORM_LEGAL_FORM")]
    [TestCase("PLATFORM_SIRET")]
    [TestCase("PLATFORM_VAT_NUMBER")]
    [TestCase("PLATFORM_ADDRESS")]
    [TestCase("ADMIN_DISPUTE_EMAIL")]
    public void Load_Throws_WhenSingleRequiredVarIsMissing(string missingVar)
    {
        SetAllRequired();
        Environment.SetEnvironmentVariable(missingVar, null);

        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() => AppEnvironment.Load());

        Assert.That(ex!.Message, Does.Contain(missingVar));
    }

    [Test]
    public void Load_ThrowsWithDetails_WhenExpireMinutesIsNotNumeric()
    {
        SetAllRequired();
        Environment.SetEnvironmentVariable("JWT_EXPIRE_MINUTES", "invalid");

        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() => AppEnvironment.Load());

        Assert.That(ex!.Message, Does.Contain("JWT_EXPIRE_MINUTES"));
        Assert.That(ex.Message, Does.Contain("invalid"));
    }

    [TestCase("TRUE")]
    [TestCase("True")]
    [TestCase("true")]
    public void Load_ParsesBoolCaseInsensitively(string value)
    {
        SetAllRequired();
        Environment.SetEnvironmentVariable("OPENAPI_ENABLE_DOCUMENTATION", value);

        AppEnvironment env = AppEnvironment.Load();

        Assert.That(env.OpenApiEnableDocumentation, Is.True);
    }

    private static void SetAllRequired()
    {
        Environment.SetEnvironmentVariable("CONNECTION_STRING_DATABASE", "Host=localhost;Database=test");
        Environment.SetEnvironmentVariable("JWT_KEY", "test-key");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "test-issuer");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "test-audience");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_SERVICE_URL", "http://localhost:3900");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_ACCESS_KEY", "ak");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_SECRET_KEY", "sk");
        Environment.SetEnvironmentVariable("OBJECT_STORAGE_BUCKET_NAME", "bucket");
        Environment.SetEnvironmentVariable("RABBITMQ_HOST", "localhost");
        Environment.SetEnvironmentVariable("RABBITMQ_USER", "guest");
        Environment.SetEnvironmentVariable("RABBITMQ_PASSWORD", "guest");
        Environment.SetEnvironmentVariable("STRIPE_PUBLISHABLE_KEY", "pk_test_stripe");
        Environment.SetEnvironmentVariable("STRIPE_SECRET_KEY", "sk_test_stripe");
        Environment.SetEnvironmentVariable("STRIPE_WEBHOOK_SECRET", "whsec_test_stripe");
        Environment.SetEnvironmentVariable("PLATFORM_LEGAL_NAME", "Test Platform");
        Environment.SetEnvironmentVariable("PLATFORM_LEGAL_FORM", "SAS");
        Environment.SetEnvironmentVariable("PLATFORM_SIRET", "73282932000074");
        Environment.SetEnvironmentVariable("PLATFORM_VAT_NUMBER", "FR12345678900");
        Environment.SetEnvironmentVariable("PLATFORM_ADDRESS", "1 rue Test, 75001 Paris");
        Environment.SetEnvironmentVariable("PLATFORM_VAT_APPLICABLE", "true");
        Environment.SetEnvironmentVariable("ADMIN_DISPUTE_EMAIL", "disputes@test.local");
    }
}
