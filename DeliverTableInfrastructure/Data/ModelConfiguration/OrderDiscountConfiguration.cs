using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class OrderDiscountConfiguration : IEntityTypeConfiguration<OrderDiscount>
{
    public void Configure(EntityTypeBuilder<OrderDiscount> builder)
    {
        builder.HasKey(od => od.Id);

        builder.HasOne(od => od.Order)
            .WithMany(o => o.Discounts)
            .HasForeignKey(od => od.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(od => od.Source)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(od => od.Amount)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.HasIndex(od => od.OrderId);
    }
}
