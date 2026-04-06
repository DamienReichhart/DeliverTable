using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class ReclamationItemConfiguration : IEntityTypeConfiguration<ReclamationItem>
{
    public void Configure(EntityTypeBuilder<ReclamationItem> builder)
    {
        builder.HasKey(r => r.Id);
        builder.HasOne(r => r.Reclamation).WithMany(r => r.Items).HasForeignKey(r => r.ReclamationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.OrderItem).WithMany().HasForeignKey(r => r.OrderItemId).OnDelete(DeleteBehavior.Cascade);
    }
}