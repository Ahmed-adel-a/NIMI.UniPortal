using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Audit.Application.Abstractions;
using NIMI.UniPortal.Modules.Audit.Domain;

namespace NIMI.UniPortal.Modules.Audit.Infrastructure.Persistence;

/// <summary>The Audit module's own schema (P10.2, schema <c>audit</c>) — ids of other modules only.</summary>
public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options)
    : DbContext(options), IAuditDbContext
{
    public const string Schema = "audit";

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditDbContext).Assembly);
    }
}
