using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class RestaurantOwnerConfiguration : IEntityTypeConfiguration<RestaurantOwner>
{
    public void Configure(EntityTypeBuilder<RestaurantOwner> builder)
    {
        builder.HasKey(ro => ro.Id);

        builder.HasOne(ro => ro.User)
            .WithOne(u => u.RestaurantOwner)
            .HasForeignKey<RestaurantOwner>(ro => ro.Id)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(u => u.CompanyName).HasMaxLength(255);
        
        builder.Property(u => u.VatNumber).HasMaxLength(20);
        
        builder.Property(u => u.ContactPhoneNumber).HasMaxLength(20);
        
        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.UpdatedAt)
            .HasDefaultValueSql("NOW()")
            .ValueGeneratedOnAddOrUpdate();
    }
}