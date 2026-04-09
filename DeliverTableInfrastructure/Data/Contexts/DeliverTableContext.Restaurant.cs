using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Data
{
    public partial class DeliverTableContext
    {
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<Dish> Dishes { get; set; }
        public DbSet<RestaurantTable> RestaurantTables { get; set; }
        public DbSet<OrderRule> OrderRules { get; set; }
        public DbSet<OrderBlockedSlot> OrderBlockedSlots { get; set; }
    }
}