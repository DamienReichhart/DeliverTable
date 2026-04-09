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
        "RABBITMQ_HOST", "RABBITMQ_USER", "RABBITMQ_PASSWORD"
    ];

    private static readonly string[] AllOptionalVars =
    [
        "CONNECTION_STRING_REDIS",
        "JWT_EXPIRE_MINUTES",
        "OBJECT_STORAGE_FORCE_PATH_STYLE",
        "OPENAPI_ENABLE_DOCUMENTATION",
        "CORS_ALLOWED_ORIGINS",
        "UPLOAD_MAX_SIZE_MB",
        "RABBITMQ_PORT"
    ];

    [TearDown]
    public void TearDown()
    {
        foreach (var name in AllRequiredVars)
            Environment.SetEnvironmentVariable(name, null);
        foreach (var name in AllOptionalVars)
            Environment.SetEnvironmentVariable(name, null);
    }

    [Test]
    public void Load_ReadsAllRequiredVariables()
    {
        SetAllRequired();

        var env = AppEnvironment.Load();

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
        });
    }

    [Test]
    public void Load_AppliesDefaults_WhenOptionalVarsAreMissing()
    {
        SetAllRequired();

        var env = AppEnvironment.Load();

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

        var env = AppEnvironment.Load();

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
        var ex = Assert.Throws<InvalidOperationException>(() => AppEnvironment.Load());

        Assert.Multiple(() =>
        {
            foreach (var name in AllRequiredVars)
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
    public void Load_Throws_WhenSingleRequiredVarIsMissing(string missingVar)
    {
        SetAllRequired();
        Environment.SetEnvironmentVariable(missingVar, null);

        var ex = Assert.Throws<InvalidOperationException>(() => AppEnvironment.Load());

        Assert.That(ex!.Message, Does.Contain(missingVar));
    }

    [Test]
    public void Load_ThrowsWithDetails_WhenExpireMinutesIsNotNumeric()
    {
        SetAllRequired();
        Environment.SetEnvironmentVariable("JWT_EXPIRE_MINUTES", "invalid");

        var ex = Assert.Throws<InvalidOperationException>(() => AppEnvironment.Load());

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

        var env = AppEnvironment.Load();

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
    }
}
