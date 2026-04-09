using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class EventBookingPolicyConfiguration : IEntityTypeConfiguration<EventBookingPolicy>
{
    public void Configure(EntityTypeBuilder<EventBookingPolicy> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.Event)
            .WithMany(e => e.EventBookingPolicies)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.MinConfirmAmount)
            .HasColumnType("decimal(9, 2)");

        builder.Property(p => p.PolicySchema)
            .HasMaxLength(2000)
            .HasDefaultValue(string.Empty);
    }
}
