using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class DiscountCodeRedemptionConfiguration : IEntityTypeConfiguration<DiscountCodeRedemption>
{
    public void Configure(EntityTypeBuilder<DiscountCodeRedemption> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.DiscountCode)
            .WithMany(dc => dc.Redemptions)
            .HasForeignKey(r => r.DiscountCodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.HasIndex(r => new { r.DiscountCodeId, r.CustomerId });
    }
}
