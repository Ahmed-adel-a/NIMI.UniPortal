using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Enrollment;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Plans;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;

/// <summary>
/// The SemesterBased module's data seam (P4.B, schema <c>lifecycle_semester</c>). Commands change tracked entities
/// and let the module's UnitOfWork save exactly once; event handlers save themselves.
/// </summary>
public interface ISemesterDbContext
{
    DbSet<StudyPlan> StudyPlans { get; }

    DbSet<PromotionPolicy> PromotionPolicies { get; }

    DbSet<StudentStanding> Standings { get; }

    DbSet<TermEnrollment> TermEnrollments { get; }

    DbSet<YearPromotion> YearPromotions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
