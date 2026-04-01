using Microsoft.EntityFrameworkCore;
using NymrSignIn.Domain.Register;

namespace NymrSignIn.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public DbSet<SiteRegisterEntry> SiteRegisterEntries => Set<SiteRegisterEntry>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("kiosk");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
