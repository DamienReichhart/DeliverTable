using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class InvoiceCounterConfiguration : IEntityTypeConfiguration<InvoiceCounter>
{
    public void Configure(EntityTypeBuilder<InvoiceCounter> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => new { c.EntityType, c.EntityId, c.Year }).IsUnique();
        builder.Property(c => c.EntityType).HasConversion<string>();
        builder.Property(c => c.NextNumber).HasDefaultValue(1);
    }
}
