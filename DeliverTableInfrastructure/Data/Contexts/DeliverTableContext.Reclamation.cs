using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Data;

public partial class DeliverTableContext
{
    public DbSet<Reclamation> Reclamations { get; set; }
    public DbSet<ReclamationItem> ReclamationItems { get; set; }
}