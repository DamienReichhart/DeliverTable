using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class EmailJobConfiguration : IEntityTypeConfiguration<EmailJob>
{
    public void Configure(EntityTypeBuilder<EmailJob> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(j => j.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(j => j.RecipientEmail)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(j => j.RecipientName)
            .HasMaxLength(200);

        builder.Property(j => j.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(j => j.TemplateData)
            .IsRequired();

        builder.Property(j => j.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(j => j.MaxRetries)
            .IsRequired()
            .HasDefaultValue(5);

        builder.Property(j => j.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(j => j.AttachmentStoragePath)
            .HasMaxLength(400);

        builder.Property(j => j.AttachmentFilename)
            .HasMaxLength(200);

        builder.Property(j => j.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => new { j.Status, j.CreatedAt });
    }
}
