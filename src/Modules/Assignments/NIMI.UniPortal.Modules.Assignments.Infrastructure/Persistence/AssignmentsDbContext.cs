using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Assignments.Application.Abstractions;
using NIMI.UniPortal.Modules.Assignments.Domain;

namespace NIMI.UniPortal.Modules.Assignments.Infrastructure.Persistence;

/// <summary>The Assignments module's own schema (P8, schema <c>assignments</c>) — ids of other modules only.</summary>
public sealed class AssignmentsDbContext(DbContextOptions<AssignmentsDbContext> options)
    : DbContext(options), IAssignmentsDbContext
{
    public const string Schema = "assignments";

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<Submission> Submissions => Set<Submission>();

    internal DbSet<ReceiptCounter> ReceiptCounters => Set<ReceiptCounter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssignmentsDbContext).Assembly);
    }
}
