using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableInfrastructure.Data.ModelConfiguration;

public class PromotionDishConfiguration : IEntityTypeConfiguration<PromotionDish>
{
    public void Configure(EntityTypeBuilder<PromotionDish> builder)
    {
        builder.HasKey(pd => pd.Id);

        builder.HasOne(pd => pd.Promotion)
            .WithMany(p => p.PromotionDishes)
            .HasForeignKey(pd => pd.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pd => pd.Dish)
            .WithMany()
            .HasForeignKey(pd => pd.DishId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pd => new { pd.PromotionId, pd.DishId }).IsUnique();
    }
}
