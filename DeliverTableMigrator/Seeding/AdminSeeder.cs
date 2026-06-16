using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableMigrator.Seeding;

/// <summary>
///     Default <see cref="IAdminSeeder"/> implementation backed by <see cref="IUserRepository"/>.
///     Creation is skipped when any user already holds the Administrator role, so running the
///     migrator repeatedly is safe and never produces a second bootstrap admin.
/// </summary>
public sealed class AdminSeeder(IUserRepository userRepository) : IAdminSeeder
{
    public async Task<AdminSeedResult> SeedAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken ct = default)
    {
        List<User> existingAdmins =
            await userRepository.ListByRoleAsync(nameof(UserRole.Administrator), ct);
        if (existingAdmins.Count > 0)
        {
            return AdminSeedResult.AlreadyExists();
        }

        User admin = new User
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Status = UserStatus.Active,
        };

        (bool created, IEnumerable<string> createErrors) =
            await userRepository.CreateAsync(admin, password);
        if (!created)
        {
            return AdminSeedResult.Failed(createErrors);
        }

        (bool roleAssigned, IEnumerable<string> roleErrors) =
            await userRepository.AddToRoleAsync(admin, nameof(UserRole.Administrator));
        if (!roleAssigned)
        {
            return AdminSeedResult.Failed(roleErrors);
        }

        return AdminSeedResult.Created();
    }
}
