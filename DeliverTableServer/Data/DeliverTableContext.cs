using DeliverTableServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Data;

public partial class DeliverTableContext(DbContextOptions<DeliverTableContext> options) : IdentityDbContext<User, IdentityRole<int>, int>(options)
{
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