using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Graduation;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Results;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure.Persistence;

/// <summary>
/// The CreditHours lifecycle variant's schema (P4, schema <c>lifecycle_credit</c>). Register and drop open one
/// explicit transaction and then let the UnitOfWork call <see cref="SaveChangesAsync"/>; the override commits that
/// transaction after the single save, so the seat counter and the registration row change atomically.
/// </summary>
public sealed class CreditHoursDbContext(DbContextOptions<CreditHoursDbContext> options)
    : DbContext(options), ICreditHoursDbContext
{
    /// <summary>This variant's PostgreSQL schema — no cross-schema foreign keys (decision LIFECYCLE).</summary>
    public const string Schema = "lifecycle_credit";

    public DbSet<RegistrationWindow> RegistrationWindows => Set<RegistrationWindow>();

    public DbSet<SubjectPrerequisite> SubjectPrerequisites => Set<SubjectPrerequisite>();

    public DbSet<GroupSeat> GroupSeats => Set<GroupSeat>();

    public DbSet<CourseRegistration> CourseRegistrations => Set<CourseRegistration>();

    public DbSet<CreditLedgerEntry> CreditLedger => Set<CreditLedgerEntry>();

    public DbSet<GpaSnapshot> GpaSnapshots => Set<GpaSnapshot>();

    public DbSet<GraduationRequirement> GraduationRequirements => Set<GraduationRequirement>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var saved = await base.SaveChangesAsync(cancellationToken);

        // The register/drop commands begin this transaction so the raw seat UPDATE and the row change commit together.
        // Commands without one (windows, prerequisites) simply save.
        if (Database.CurrentTransaction is { } transaction)
            await transaction.CommitAsync(cancellationToken);

        return saved;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CreditHoursDbContext).Assembly);
    }
}
