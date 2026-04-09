using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(oi => oi.Id);

        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oi => oi.Dish)
            .WithMany()
            .HasForeignKey(oi => oi.DishId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(oi => oi.DishName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(oi => oi.Quantity)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(oi => oi.UnitPrice)
            .HasColumnType("decimal(7, 2)")
            .IsRequired();

        builder.Property(oi => oi.SpecialInstructions)
            .HasMaxLength(500)
            .HasDefaultValue(string.Empty);
    }
}
