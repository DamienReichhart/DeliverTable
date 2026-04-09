using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Data
{
    public partial class DeliverTableContext
    {
        public DbSet<RestaurantRating> RestaurantRatings { get; set; }
        public DbSet<CustomerFavouriteRestaurant> CustomerFavouriteRestaurants { get; set; }
        public DbSet<CustomerHiddenRestaurant> CustomerHiddenRestaurants { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ModerationAction> ModerationActions { get; set; }
    }
}
