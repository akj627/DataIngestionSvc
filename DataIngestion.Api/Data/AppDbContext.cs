using DataIngestion.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DataIngestion.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Holding> Holdings => Set<Holding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>()
            .HasIndex(c => c.ClientId)
            .IsUnique();

        modelBuilder.Entity<Account>()
            .HasIndex(a => a.AccountId)
            .IsUnique();
    }
}
