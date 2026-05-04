using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Kind)
            .HasConversion<int>()
            .HasDefaultValue(InvoiceLineKind.Item)
            .IsRequired();
    }
}
