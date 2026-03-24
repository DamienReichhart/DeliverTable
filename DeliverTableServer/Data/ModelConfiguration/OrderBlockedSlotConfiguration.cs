using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class OrderBlockedSlotConfiguration : IEntityTypeConfiguration<OrderBlockedSlot>
{
    public void Configure(EntityTypeBuilder<OrderBlockedSlot> builder)
    {
        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.Restaurant)
            .WithMany()
            .HasForeignKey(s => s.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.RestaurantTable)
            .WithMany()
            .HasForeignKey(s => s.RestaurantTableId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(s => s.StartsAt)
            .IsRequired();

        builder.Property(s => s.EndsAt)
            .IsRequired();

        builder.Property(s => s.Reason)
            .HasMaxLength(500)
            .HasDefaultValue(string.Empty);

        builder.Property(s => s.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
    }
}
