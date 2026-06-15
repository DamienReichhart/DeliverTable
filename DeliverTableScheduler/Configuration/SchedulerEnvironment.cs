namespace DeliverTableScheduler.Configuration;

public sealed class SchedulerEnvironment
{
    public required string ConnectionStringDatabase { get; init; }
    public required string StripeSecretKey { get; init; }

    public static SchedulerEnvironment Load()
    {
        List<string> errors = new List<string>();
        string cs = Require("CONNECTION_STRING_DATABASE", errors);
        string sk = Require("STRIPE_SECRET_KEY", errors);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join("; ", errors));
        return new SchedulerEnvironment { ConnectionStringDatabase = cs, StripeSecretKey = sk };
    }

    private static string Require(string name, List<string> errors)
    {
        string? v = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(v))
        {
            errors.Add($"Missing env var: {name}");
            return "";
        }

        return v;
    }
}
