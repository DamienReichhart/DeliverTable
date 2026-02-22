using DeliverTableServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Data;

public class DeliverTableContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public DeliverTableContext(DbContextOptions<DeliverTableContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<RestaurantOwner> RestaurantOwners { get; set; }
    public DbSet<CustomerProfile> CustomerProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<IdentityPasskeyData>();

        builder.ApplyConfigurationsFromAssembly(typeof(DeliverTableContext).Assembly);
    }
}