using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class OrderRuleConfiguration : IEntityTypeConfiguration<OrderRule>
{
    public void Configure(EntityTypeBuilder<OrderRule> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.Restaurant)
            .WithMany()
            .HasForeignKey(r => r.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(r => r.MinConfirmAmount)
            .HasColumnType("decimal(9, 2)");

        builder.Property(r => r.AvailabilityRanges)
            .HasMaxLength(2000)
            .HasDefaultValue(string.Empty);

        builder.Property(r => r.AllowPreorder)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.AllowDelivery)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();
    }
}
