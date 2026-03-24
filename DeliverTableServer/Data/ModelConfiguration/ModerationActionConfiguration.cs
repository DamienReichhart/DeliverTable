using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class ModerationActionConfiguration : IEntityTypeConfiguration<ModerationAction>
{
    public void Configure(EntityTypeBuilder<ModerationAction> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.AdminUser)
            .WithMany()
            .HasForeignKey(m => m.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.TargetType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(m => m.TargetId)
            .IsRequired();

        builder.Property(m => m.ActionType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(m => m.Reason)
            .HasMaxLength(2000)
            .HasDefaultValue(string.Empty);

        builder.Property(m => m.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
    }
}
