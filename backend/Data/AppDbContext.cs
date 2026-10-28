using IncidentCommander.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IncidentCommander.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Incident> Incidents => Set<Incident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Incident>().HasKey(i => i.Id);
    }
}
