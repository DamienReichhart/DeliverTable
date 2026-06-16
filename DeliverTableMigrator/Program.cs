using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableMigrator.Configuration;
using DeliverTableMigrator.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

DotNetEnv.Env.Load();
MigratorEnvironment env = MigratorEnvironment.Load();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<DeliverTableContext>(options =>
    options.UseNpgsql(env.ConnectionStringDatabase));

// Identity core + EF stores give us a UserManager that hashes passwords and assigns
// roles exactly like the running API (same 12-char password policy).
builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 12;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<DeliverTableContext>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAdminSeeder, AdminSeeder>();

using IHost host = builder.Build();
ILogger logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Migrator");

await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
DeliverTableContext db = scope.ServiceProvider.GetRequiredService<DeliverTableContext>();

logger.LogInformation("Applying database migrations...");
await db.Database.MigrateAsync();
logger.LogInformation("Database migrations applied.");

IAdminSeeder seeder = scope.ServiceProvider.GetRequiredService<IAdminSeeder>();
AdminSeedResult result = await seeder.SeedAsync(
    env.AdminEmail, env.AdminPassword, env.AdminFirstName, env.AdminLastName);

switch (result.Outcome)
{
    case AdminSeedOutcome.Created:
        logger.LogInformation("Bootstrap administrator '{Email}' created.", env.AdminEmail);
        return 0;

    case AdminSeedOutcome.AlreadyExists:
        logger.LogInformation("An administrator already exists; skipping admin bootstrap.");
        return 0;

    default:
        logger.LogError(
            "Failed to create bootstrap administrator '{Email}': {Errors}",
            env.AdminEmail, string.Join("; ", result.Errors));
        return 1;
}
