namespace DeliverTableMigrator.Seeding;

/// <summary>
///     Outcome of an attempt to seed the bootstrap administrator account.
/// </summary>
public enum AdminSeedOutcome
{
    /// <summary>A new administrator account was created.</summary>
    Created,

    /// <summary>At least one administrator already existed; no account was created.</summary>
    AlreadyExists,

    /// <summary>Account creation or role assignment failed; see <see cref="AdminSeedResult.Errors"/>.</summary>
    Failed,
}

/// <summary>
///     Result of <see cref="IAdminSeeder.SeedAsync"/>.
/// </summary>
public sealed record AdminSeedResult(AdminSeedOutcome Outcome, IReadOnlyList<string> Errors)
{
    public static AdminSeedResult Created() => new(AdminSeedOutcome.Created, []);
    public static AdminSeedResult AlreadyExists() => new(AdminSeedOutcome.AlreadyExists, []);
    public static AdminSeedResult Failed(IEnumerable<string> errors) => new(AdminSeedOutcome.Failed, errors.ToList());
}

/// <summary>
///     Idempotently provisions the first administrator account from configured credentials.
///     Creating an admin is skipped when any administrator already exists.
/// </summary>
public interface IAdminSeeder
{
    Task<AdminSeedResult> SeedAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken ct = default);
}
