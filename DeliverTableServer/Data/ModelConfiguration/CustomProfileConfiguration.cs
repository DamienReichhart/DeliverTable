using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class CustomProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        builder.HasOne(ro => ro.User)
            .WithOne(u => u.CustomerProfile)
            .HasForeignKey<CustomerProfile>(ro => ro.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(cp => cp.AllergyNotes).HasMaxLength(255);
        builder.Property(cp => cp.DietaryPreferences).HasMaxLength(255);
        
        builder.Property(cp => cp.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
        builder.Property(cp => cp.UpdatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAddOrUpdate();
    }
}