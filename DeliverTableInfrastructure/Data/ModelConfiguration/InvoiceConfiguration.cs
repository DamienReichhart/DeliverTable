using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.Number).IsUnique();
        builder.HasIndex(i => i.OrderId);
        builder.HasIndex(i => i.RecipientUserId);
        builder.HasIndex(i => i.RecipientRestaurantId);

        builder.Property(i => i.Kind).HasConversion<string>();
        builder.Property(i => i.IssuerType).HasConversion<string>();
        builder.Property(i => i.Status).HasConversion<string>();

        builder.HasOne(i => i.Order)
               .WithMany()
               .HasForeignKey(i => i.OrderId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.IssuerRestaurant)
               .WithMany()
               .HasForeignKey(i => i.IssuerRestaurantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.RecipientUser)
               .WithMany()
               .HasForeignKey(i => i.RecipientUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.RecipientRestaurant)
               .WithMany()
               .HasForeignKey(i => i.RecipientRestaurantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.RelatedInvoice)
               .WithMany()
               .HasForeignKey(i => i.RelatedInvoiceId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Lines)
               .WithOne(l => l.Invoice)
               .HasForeignKey(l => l.InvoiceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
