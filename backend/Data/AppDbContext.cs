using IncidentCommander.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IncidentCommander.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Incident>().HasKey(i => i.Id);
        modelBuilder.Entity<AuditEntry>().HasKey(a => a.Id);
        modelBuilder.Entity<AuditEntry>().HasIndex(a => a.IncidentId);
    }
}
