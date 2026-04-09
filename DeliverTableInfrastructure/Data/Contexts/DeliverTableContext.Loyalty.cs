using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Data
{
    public partial class DeliverTableContext
    {
        public DbSet<LoyaltyProgram> LoyaltyPrograms { get; set; }
        public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; }
        public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
    }
}
