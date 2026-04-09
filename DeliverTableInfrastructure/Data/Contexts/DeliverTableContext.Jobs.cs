using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Data;

public partial class DeliverTableContext
{
    public DbSet<EmailJob> EmailJobs { get; set; }
}
