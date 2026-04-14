using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.StripeRefundId).HasMaxLength(200).IsRequired();
        builder.HasIndex(r => r.StripeRefundId).IsUnique();
        builder.Property(r => r.Currency).HasMaxLength(3).HasDefaultValue("EUR").IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(500).HasDefaultValue(string.Empty);
        builder.Property(r => r.Amount).HasColumnType("decimal(9, 2)").IsRequired();

        builder.HasOne(r => r.Payment)
               .WithMany(p => p.Refunds)
               .HasForeignKey(r => r.PaymentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.CreatedByUser)
               .WithMany()
               .HasForeignKey(r => r.CreatedByUserId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
