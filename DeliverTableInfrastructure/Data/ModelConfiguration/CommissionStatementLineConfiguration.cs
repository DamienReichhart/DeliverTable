using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class CommissionStatementLineConfiguration : IEntityTypeConfiguration<CommissionStatementLine>
{
    public void Configure(EntityTypeBuilder<CommissionStatementLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => l.OrderId);

        // Partial unique on RefundEventId prevents processing the same Stripe refund twice.
        builder.HasIndex(l => l.RefundEventId)
               .HasDatabaseName("UX_CommissionStatementLine_RefundEventId")
               .HasFilter("\"RefundEventId\" IS NOT NULL")
               .IsUnique();

        builder.HasOne(l => l.Order)
               .WithMany()
               .HasForeignKey(l => l.OrderId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
