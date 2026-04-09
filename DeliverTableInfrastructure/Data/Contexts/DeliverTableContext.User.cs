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
        public DbSet<Customer> Customers { get; set; }
        public DbSet<RestaurantOwner> RestaurantOwners { get; set; }
    }
}