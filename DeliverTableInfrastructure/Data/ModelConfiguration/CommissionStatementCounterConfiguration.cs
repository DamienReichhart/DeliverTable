using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class CommissionStatementCounterConfiguration : IEntityTypeConfiguration<CommissionStatementCounter>
{
    public void Configure(EntityTypeBuilder<CommissionStatementCounter> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}
