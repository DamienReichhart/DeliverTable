using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Data
{
    public partial class DeliverTableContext
    {
        public DbSet<Event> Events { get; set; }
        public DbSet<EventMenuItem> EventMenuItems { get; set; }
        public DbSet<EventBookingPolicy> EventBookingPolicies { get; set; }
    }
}
