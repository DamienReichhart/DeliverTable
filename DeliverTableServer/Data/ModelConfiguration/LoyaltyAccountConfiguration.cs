using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
    {
        builder.HasKey(la => la.Id);

        builder.HasOne(la => la.LoyaltyProgram)
            .WithMany(lp => lp.Accounts)
            .HasForeignKey(la => la.LoyaltyProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(la => la.Customer)
            .WithMany()
            .HasForeignKey(la => la.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(la => la.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(la => la.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(la => new { la.LoyaltyProgramId, la.CustomerId }).IsUnique();
    }
}
