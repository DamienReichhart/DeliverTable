using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class CustomerHiddenRestaurantConfiguration
    : IEntityTypeConfiguration<CustomerHiddenRestaurant>
{
    public void Configure(EntityTypeBuilder<CustomerHiddenRestaurant> builder)
    {
        builder.HasKey(h => new { h.CustomerUserId, h.RestaurantId });

        builder.HasOne(h => h.CustomerUser)
            .WithMany()
            .HasForeignKey(h => h.CustomerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Restaurant)
            .WithMany()
            .HasForeignKey(h => h.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(h => h.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
    }
}
