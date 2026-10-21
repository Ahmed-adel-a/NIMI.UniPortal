using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;

namespace NIMI.UniPortal.Modules.Scheduling.Infrastructure.Persistence;

public sealed class SchedulingDbContext(DbContextOptions<SchedulingDbContext> options)
    : DbContext(options), ISchedulingDbContext
{
    public const string Schema = "scheduling";

    public DbSet<Term> Terms => Set<Term>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);
    }
}
