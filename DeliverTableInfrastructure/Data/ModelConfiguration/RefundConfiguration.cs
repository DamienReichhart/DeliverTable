using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.StripeRefundId).IsUnique();
        builder.Property(r => r.Currency).HasDefaultValue("EUR");
        builder.Property(r => r.Reason).HasDefaultValue(string.Empty);

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
