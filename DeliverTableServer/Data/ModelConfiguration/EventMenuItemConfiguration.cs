using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class EventMenuItemConfiguration : IEntityTypeConfiguration<EventMenuItem>
{
    public void Configure(EntityTypeBuilder<EventMenuItem> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Event)
            .WithMany(ev => ev.EventMenuItems)
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Dish)
            .WithMany()
            .HasForeignKey(e => e.DishId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.OverridePrice)
            .HasColumnType("decimal(7, 2)");
    }
}
