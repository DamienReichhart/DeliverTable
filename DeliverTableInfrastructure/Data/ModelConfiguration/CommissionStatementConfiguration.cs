using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class CommissionStatementConfiguration : IEntityTypeConfiguration<CommissionStatement>
{
    public void Configure(EntityTypeBuilder<CommissionStatement> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.Number).IsUnique();
        builder.HasIndex(s => new { s.RecipientRestaurantId, s.PeriodYear, s.PeriodMonth });

        // One Invoice-kind statement per restaurant per period (partial unique index, Postgres only).
        builder.HasIndex(s => new { s.RecipientRestaurantId, s.PeriodYear, s.PeriodMonth })
               .HasDatabaseName("UX_CommissionStatement_Restaurant_Period_Invoice")
               .HasFilter("\"Kind\" = 'Invoice'")
               .IsUnique();

        builder.Property(s => s.Kind).HasConversion<string>();
        builder.Property(s => s.Status).HasConversion<string>();

        builder.HasOne(s => s.RecipientRestaurant)
               .WithMany()
               .HasForeignKey(s => s.RecipientRestaurantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.RelatedStatement)
               .WithMany()
               .HasForeignKey(s => s.RelatedStatementId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Lines)
               .WithOne(l => l.CommissionStatement)
               .HasForeignKey(l => l.CommissionStatementId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
