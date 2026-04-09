using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Restaurant)
            .WithMany()
            .HasForeignKey(o => o.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(o => o.OrderType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(o => o.PaymentStatus)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(o => o.TotalAmount)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.Property(o => o.GuestCount)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(o => o.DeliveryAddress)
            .HasMaxLength(500)
            .HasDefaultValue(string.Empty);

        builder.Property(o => o.Notes)
            .HasMaxLength(500)
            .HasDefaultValue(string.Empty);

        builder.Property(o => o.Source)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(o => o.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(o => o.OriginalAmount)
            .HasColumnType("decimal(9, 2)")
            .IsRequired()
            .HasDefaultValue(0m);

        builder.Property(o => o.DiscountAmount)
            .HasColumnType("decimal(9, 2)")
            .IsRequired()
            .HasDefaultValue(0m);

        builder.Property(o => o.LoyaltyPointsUsed)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(o => o.LoyaltyPointsEarned)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(o => o.DiscountCode)
            .WithMany()
            .HasForeignKey(o => o.DiscountCodeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.RestaurantTable)
            .WithMany()
            .HasForeignKey(o => o.RestaurantTableId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Event)
            .WithMany()
            .HasForeignKey(o => o.EventId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(o => o.IsEventBooking)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.RestaurantId);
        builder.HasIndex(o => o.Status);
    }
}
