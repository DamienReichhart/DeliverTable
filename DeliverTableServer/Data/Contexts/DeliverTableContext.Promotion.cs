using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Data
{
    public partial class DeliverTableContext
    {
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<PromotionDish> PromotionDishes { get; set; }
        public DbSet<DiscountCode> DiscountCodes { get; set; }
        public DbSet<DiscountCodeRedemption> DiscountCodeRedemptions { get; set; }
        public DbSet<OrderDiscount> OrderDiscounts { get; set; }
    }
}
