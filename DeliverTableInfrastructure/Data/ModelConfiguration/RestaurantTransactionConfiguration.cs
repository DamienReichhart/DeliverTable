using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class RestaurantTransactionConfiguration : IEntityTypeConfiguration<RestaurantTransaction>
{
    public void Configure(EntityTypeBuilder<RestaurantTransaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasOne(t => t.Restaurant)
            .WithMany(r => r.Transactions)
            .HasForeignKey(t => t.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Order)
            .WithMany()
            .HasForeignKey(t => t.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(t => t.GrossAmount)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.Property(t => t.CommissionAmount)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.Property(t => t.NetAmount)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.Property(t => t.BalanceAfter)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.HasIndex(t => t.RestaurantId);
        builder.HasIndex(t => t.OrderId);
        builder.HasIndex(t => t.Type);
    }
}
