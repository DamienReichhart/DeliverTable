using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class CustomerFavouriteRestaurantConfiguration
    : IEntityTypeConfiguration<CustomerFavouriteRestaurant>
{
    public void Configure(EntityTypeBuilder<CustomerFavouriteRestaurant> builder)
    {
        builder.HasKey(f => new { f.CustomerUserId, f.RestaurantId });

        builder.HasOne(f => f.CustomerUser)
            .WithMany()
            .HasForeignKey(f => f.CustomerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Restaurant)
            .WithMany()
            .HasForeignKey(f => f.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(f => f.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
    }
}
