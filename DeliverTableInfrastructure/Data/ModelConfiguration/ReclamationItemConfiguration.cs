using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class ReclamationItemConfiguration : IEntityTypeConfiguration<ReclamationItem>
{
    public void Configure(EntityTypeBuilder<ReclamationItem> builder)
    {
        builder.HasOne(r => r.Reclamation).WithMany().HasForeignKey(r => r.ReclamationId);
        builder.HasOne(r => r.Order).WithMany().HasForeignKey(r => r.OrderId);
    }
}