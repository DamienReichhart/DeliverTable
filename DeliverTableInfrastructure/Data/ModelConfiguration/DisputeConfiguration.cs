using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => d.StripeDisputeId).IsUnique();
        builder.HasIndex(d => d.PaymentId);
        builder.HasIndex(d => d.OrderId);
        builder.HasIndex(d => d.RestaurantId);
        builder.HasIndex(d => new { d.RestaurantId, d.State });

        builder.Property(d => d.State).HasConversion<string>();

        builder.HasOne(d => d.Payment)
               .WithMany()
               .HasForeignKey(d => d.PaymentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Order)
               .WithMany()
               .HasForeignKey(d => d.OrderId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Restaurant)
               .WithMany()
               .HasForeignKey(d => d.RestaurantId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
