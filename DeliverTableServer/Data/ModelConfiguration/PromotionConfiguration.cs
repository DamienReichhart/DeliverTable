using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.Restaurant)
            .WithMany()
            .HasForeignKey(p => p.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.PromotionType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.DiscountType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.DiscountValue)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.Property(p => p.MinOrderAmount)
            .HasColumnType("decimal(9, 2)");

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(p => p.RestaurantId);
        builder.HasIndex(p => p.IsActive);
    }
}
