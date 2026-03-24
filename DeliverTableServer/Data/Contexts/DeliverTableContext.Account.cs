using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Data
{
    public partial class DeliverTableContext
    {
        public DbSet<RestaurantTransaction> RestaurantTransactions { get; set; }
    }
}
