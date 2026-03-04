using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration
{
    public class DishConfiguration : IEntityTypeConfiguration<Dish>
    {
        public void Configure(EntityTypeBuilder<Dish> builder)
        {
            builder.HasKey(d => d.Id);

            builder.HasOne(d => d.Restaurant)
                .WithMany(r => r.Dishes)
                .HasForeignKey(d => d.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(d => d.Description)
                .HasMaxLength(1000)
                .HasDefaultValue(string.Empty);

            builder.Property(d => d.BasePrice)
                .HasColumnType("decimal(7, 2)")
                .HasDefaultValue(0);

            builder.Property(d => d.IsVegetarian)
                .HasDefaultValue(false);

            builder.Property(d => d.IsVegan)
                .HasDefaultValue(false);

            builder.Property(d => d.IsGlutenFree)
                .HasDefaultValue(false);

            builder.Property(d => d.IsAllergenHazard)
                .HasDefaultValue(false);

            builder.Property(d => d.IsDishOfTheDay)
                .HasDefaultValue(false);

            builder.Property(d => d.IsActive)
                .HasDefaultValue(true);

            builder.Property(d => d.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            builder.Property(d => d.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();
        }
    }
}