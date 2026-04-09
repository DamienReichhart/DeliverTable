using DeliverTableServer.Configuration;

namespace DeliverTableTests.Global.Helpers;

public static class AppEnvironmentTestHelper
{
    private static readonly Dictionary<string, string> RequiredVars = new()
    {
        ["CONNECTION_STRING_DATABASE"] = "Host=localhost;Database=test",
        ["JWT_KEY"] = "TestKeyThatIsLongEnoughForHmacSha256Signing!",
        ["JWT_ISSUER"] = "TestIssuer",
        ["JWT_AUDIENCE"] = "TestAudience",
        ["OBJECT_STORAGE_SERVICE_URL"] = "http://localhost:3900",
        ["OBJECT_STORAGE_ACCESS_KEY"] = "key",
        ["OBJECT_STORAGE_SECRET_KEY"] = "secret",
        ["OBJECT_STORAGE_BUCKET_NAME"] = "bucket",
        ["PLATFORM_COMMISSION_RATE"] = "0.10",
        ["RABBITMQ_HOST"] = "localhost",
        ["RABBITMQ_USER"] = "guest",
        ["RABBITMQ_PASSWORD"] = "guest"
    };

    public static AppEnvironment SetupEnvironment()
    {
        foreach (var (key, value) in RequiredVars)
            Environment.SetEnvironmentVariable(key, value);

        return AppEnvironment.Load();
    }

    public static void CleanupEnvironment()
    {
        foreach (var key in RequiredVars.Keys)
            Environment.SetEnvironmentVariable(key, null);
    }
}
