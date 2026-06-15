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
        ["RABBITMQ_PASSWORD"] = "guest",
        ["STRIPE_PUBLISHABLE_KEY"] = "pk_test_stripe",
        ["STRIPE_SECRET_KEY"] = "sk_test_stripe",
        ["STRIPE_WEBHOOK_SECRET"] = "whsec_test_stripe",
        ["PLATFORM_LEGAL_NAME"] = "Test Platform",
        ["PLATFORM_LEGAL_FORM"] = "SAS",
        ["PLATFORM_SIRET"] = "73282932000074",
        ["PLATFORM_VAT_NUMBER"] = "FR12345678900",
        ["PLATFORM_ADDRESS"] = "1 rue Test, 75001 Paris",
        ["PLATFORM_VAT_APPLICABLE"] = "true",
        ["ADMIN_DISPUTE_EMAIL"] = "disputes@test.local"
    };

    public static AppEnvironment SetupEnvironment()
    {
        foreach ((string? key, string? value) in RequiredVars)
            Environment.SetEnvironmentVariable(key, value);

        return AppEnvironment.Load();
    }

    public static void CleanupEnvironment()
    {
        foreach (string key in RequiredVars.Keys)
            Environment.SetEnvironmentVariable(key, null);
    }
}
