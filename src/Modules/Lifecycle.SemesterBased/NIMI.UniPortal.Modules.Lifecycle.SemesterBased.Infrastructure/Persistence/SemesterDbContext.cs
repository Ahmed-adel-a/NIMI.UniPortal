using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Enrollment;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Plans;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Infrastructure.Persistence;

/// <summary>The SemesterBased lifecycle variant's schema (P4.B, schema <c>lifecycle_semester</c>).</summary>
public sealed class SemesterDbContext(DbContextOptions<SemesterDbContext> options) : DbContext(options), ISemesterDbContext
{
    /// <summary>This variant's PostgreSQL schema: no cross-schema foreign keys (decision LIFECYCLE).</summary>
    public const string Schema = "lifecycle_semester";

    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();

    public DbSet<PromotionPolicy> PromotionPolicies => Set<PromotionPolicy>();

    public DbSet<StudentStanding> Standings => Set<StudentStanding>();

    public DbSet<TermEnrollment> TermEnrollments => Set<TermEnrollment>();

    public DbSet<YearPromotion> YearPromotions => Set<YearPromotion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SemesterDbContext).Assembly);
    }
}
