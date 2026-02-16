using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Data;

public class DeliverTableContext : DbContext
{
    public DeliverTableContext(DbContextOptions<DeliverTableContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        
        modelBuilder.Entity<User>()
            .Property(u => u.role)
            .HasConversion<string>();

        modelBuilder.Entity<User>()
            .Property(u => u.status)
            .HasConversion<string>();

        modelBuilder.Entity<User>()
            .Property(u => u.created_at)
            .HasDefaultValueSql("NOW()")
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<User>()
            .Property(u => u.updated_at)
            .HasDefaultValueSql("NOW()")
            .ValueGeneratedOnAddOrUpdate();
    }
}