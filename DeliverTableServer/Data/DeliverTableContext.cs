using DeliverTableServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Data;

public class DeliverTableContext(DbContextOptions<DeliverTableContext> options) : IdentityDbContext<User, IdentityRole<int>, int>(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<RestaurantOwner> RestaurantOwners { get; set; }
    public DbSet<CustomerProfile> CustomerProfiles { get; set; }
    public DbSet<Restaurant> Restaurants { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        List<IdentityRole<int>> roles =
            [
                new() {
                    Id=1,
                    Name = "Administrator",
                    NormalizedName = "ADMINISTRATOR",
                    ConcurrencyStamp = "1"
                },

                new() {
                    Id=2,
                    Name = "Customer",
                    NormalizedName = "CUSTOMER",
                    ConcurrencyStamp = "2"
                },

                new() {
                    Id=3,
                    Name = "RestaurantOwner",
                    NormalizedName = "RESTAURANT_OWNER",
                    ConcurrencyStamp = "3"
                }
            ];

        builder.Entity<IdentityRole<int>>().HasData(roles);

        builder.Ignore<IdentityPasskeyData>();

        builder.ApplyConfigurationsFromAssembly(typeof(DeliverTableContext).Assembly);
    }
}