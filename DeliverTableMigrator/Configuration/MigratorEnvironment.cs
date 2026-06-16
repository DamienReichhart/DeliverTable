namespace DeliverTableMigrator.Configuration;

/// <summary>
///     Minimal, immutable snapshot of the environment variables the migrator needs:
///     the database connection string and the bootstrap admin credentials.
///     Fails fast with an aggregated error message when a required variable is missing.
/// </summary>
public sealed class MigratorEnvironment
{
    public required string ConnectionStringDatabase { get; init; }
    public required string AdminEmail { get; init; }
    public required string AdminPassword { get; init; }
    public required string AdminFirstName { get; init; }
    public required string AdminLastName { get; init; }

    public static MigratorEnvironment Load()
    {
        List<string> errors = new List<string>();

        string connectionString = Require("CONNECTION_STRING_DATABASE", errors);
        string adminEmail = Require("BASE_ADMIN_EMAIL", errors);
        string adminPassword = Require("BASE_ADMIN_PASSWORD", errors);
        string adminFirstName = Optional("BASE_ADMIN_FIRST_NAME", "Admin");
        string adminLastName = Optional("BASE_ADMIN_LAST_NAME", "DeliverTable");

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing or invalid environment variables:\n- {string.Join("\n- ", errors)}");
        }

        return new MigratorEnvironment
        {
            ConnectionStringDatabase = connectionString,
            AdminEmail = adminEmail,
            AdminPassword = adminPassword,
            AdminFirstName = adminFirstName,
            AdminLastName = adminLastName,
        };
    }

    private static string Require(string name, List<string> errors)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(name);
            return "";
        }

        return value;
    }

    private static string Optional(string name, string defaultValue)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }
}
