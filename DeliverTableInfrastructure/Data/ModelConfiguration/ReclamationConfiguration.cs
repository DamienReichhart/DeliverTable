using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class ReclamationConfiguration : IEntityTypeConfiguration<Reclamation>
{
    public void Configure(EntityTypeBuilder<Reclamation> builder)
    {
        builder.HasKey(e => e.ReclamationId);

        builder.Property(e => e.ReclamationId).HasColumnName("ReclamationId");

        builder.HasOne(r => r.Order).WithMany(o => o.Reclamations).HasForeignKey(r => r.OrderId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Items).WithOne().HasForeignKey(i => i.ReclamationId);

        builder.HasMany(r => r.Items).WithOne().HasForeignKey(i => i.ReclamationId);

        builder.Property(r => r.Created).HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(r => r.Updated)
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.Description).HasMaxLength(10000);
    }
}