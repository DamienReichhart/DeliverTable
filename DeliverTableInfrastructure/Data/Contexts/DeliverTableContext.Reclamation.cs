using DeliverTableInfrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Data;

public partial class DeliverTableContext
{
    public DbSet<Reclamation> Reclamations { get; set; }
    public DbSet<ReclamationItem> ReclamationItems { get; set; }
}