using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class ProcessedStripeEventConfiguration : IEntityTypeConfiguration<ProcessedStripeEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedStripeEvent> builder)
    {
        builder.HasKey(e => e.StripeEventId);
        builder.Property(e => e.StripeEventId).HasMaxLength(200).IsRequired();
        builder.Property(e => e.EventType).HasMaxLength(100).IsRequired();
    }
}
