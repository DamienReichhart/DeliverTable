using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration
{
    public class CustomProfileConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.HasOne(ro => ro.User)
                .WithOne(u => u.Customer)
                .HasForeignKey<Customer>(ro => ro.Id)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(cp => cp.AllergyNotes).HasMaxLength(255);
            builder.Property(cp => cp.DietaryPreferences).HasMaxLength(255);

            builder.Property(cp => cp.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
            builder.Property(cp => cp.UpdatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAddOrUpdate();
        }
    }
}