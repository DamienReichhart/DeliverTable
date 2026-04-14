using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Data;

public partial class DeliverTableContext(DbContextOptions<DeliverTableContext> options) : IdentityDbContext<User, IdentityRole<int>, int>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        List<IdentityRole<int>> roles =
            [
                new() {
                    Id=1,
                    Name = nameof(UserRole.Administrator),
                    NormalizedName = nameof(UserRole.Administrator).ToUpperInvariant(),
                    ConcurrencyStamp = "1"
                },

                new() {
                    Id=2,
                    Name = nameof(UserRole.Customer),
                    NormalizedName = nameof(UserRole.Customer).ToUpperInvariant(),
                    ConcurrencyStamp = "2"
                },

                new() {
                    Id=3,
                    Name = nameof(UserRole.RestaurantOwner),
                    NormalizedName = nameof(UserRole.RestaurantOwner).ToUpperInvariant(),
                    ConcurrencyStamp = "3"
                }
            ];

        builder.Entity<IdentityRole<int>>().HasData(roles);

        builder.Ignore<IdentityPasskeyData>();

        builder.ApplyConfigurationsFromAssembly(typeof(DeliverTableContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is ITrackable &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            ((ITrackable)entry.Entity).Updated = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                ((ITrackable)entry.Entity).Created = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(ct);
    }
}