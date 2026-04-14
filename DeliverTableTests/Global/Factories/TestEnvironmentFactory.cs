using DeliverTableServer.Configuration;
using DeliverTableTests.Global.Helpers;

namespace DeliverTableTests.Global.Factories;

public static class TestEnvironmentFactory
{
    public static AppEnvironment Create()
    {
        try
        {
            return AppEnvironmentTestHelper.SetupEnvironment();
        }
        finally
        {
            AppEnvironmentTestHelper.CleanupEnvironment();
        }
    }
}
