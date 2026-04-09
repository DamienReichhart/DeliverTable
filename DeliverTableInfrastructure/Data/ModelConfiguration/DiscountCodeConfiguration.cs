using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
{
    public void Configure(EntityTypeBuilder<DiscountCode> builder)
    {
        builder.HasKey(dc => dc.Id);

        builder.HasOne(dc => dc.Restaurant)
            .WithMany()
            .HasForeignKey(dc => dc.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(dc => dc.DiscountType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(dc => dc.DiscountValue)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.Property(dc => dc.MinOrderAmount)
            .HasColumnType("decimal(9, 2)");

        builder.Property(dc => dc.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(dc => dc.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(dc => new { dc.RestaurantId, dc.Code }).IsUnique();
        builder.HasIndex(dc => dc.IsActive);
    }
}
