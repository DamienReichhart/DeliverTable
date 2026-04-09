using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class LoyaltyProgramConfiguration : IEntityTypeConfiguration<LoyaltyProgram>
{
    public void Configure(EntityTypeBuilder<LoyaltyProgram> builder)
    {
        builder.HasKey(lp => lp.Id);

        builder.HasOne(lp => lp.Restaurant)
            .WithMany()
            .HasForeignKey(lp => lp.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(lp => lp.PointsPerEuro)
            .HasColumnType("decimal(9, 2)")
            .IsRequired();

        builder.Property(lp => lp.EurosPerPoint)
            .HasColumnType("decimal(9, 4)")
            .IsRequired();

        builder.Property(lp => lp.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(lp => lp.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(lp => lp.RestaurantId).IsUnique();
    }
}
