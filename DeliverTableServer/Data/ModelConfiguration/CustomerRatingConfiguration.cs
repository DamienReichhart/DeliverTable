using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class CustomerRatingConfiguration : IEntityTypeConfiguration<CustomerRating>
{
    public void Configure(EntityTypeBuilder<CustomerRating> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Restaurant)
            .WithMany()
            .HasForeignKey(r => r.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RatedCustomerUser)
            .WithMany()
            .HasForeignKey(r => r.RatedCustomerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RestaurantUser)
            .WithMany()
            .HasForeignKey(r => r.RestaurantUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.Property(r => r.Comment)
            .HasMaxLength(2000)
            .HasDefaultValue(string.Empty);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
    }
}
