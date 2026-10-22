using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Policies;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Results;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Enrollment;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;
using NIMI.UniPortal.Modules.Payments.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;

/// <summary>
/// Decision SEMPROMO: applies the year-end rule to one student. The faculty's effective policy, the department's
/// final level and the outstanding fees go into <see cref="PromotionRules"/>; the standing changes and the decision
/// is recorded (not saved: the caller saves once).
/// </summary>
internal sealed class PromotionService(
    ISemesterDbContext db, IUsersApi users, IPaymentsApi payments, EffectivePolicies policies, TimeProvider clock)
{
    /// <summary>
    /// The term's students whose every enrollment has a result and who have no decision for the academic year yet
    /// (a held student is decided again only after paying, see <see cref="DecideAsync"/>).
    /// </summary>
    public async Task<IReadOnlyList<StudentStanding>> ReadyForDecisionAsync(
        Guid termId, int academicYear, IReadOnlyCollection<Guid>? studentIds, CancellationToken cancellationToken)
    {
        var enrollments = db.TermEnrollments.Where(e => e.TermId == termId && e.Status != EnrollmentStatus.Withdrawn);
        if (studentIds is not null)
        {
            var ids = studentIds.ToList();
            enrollments = enrollments.Where(e => ids.Contains(e.StudentId));
        }

        var inTerm = await enrollments.Select(e => e.StudentId).Distinct().ToListAsync(cancellationToken);
        var waiting = await enrollments
            .Where(e => e.Status == EnrollmentStatus.Enrolled)
            .Select(e => e.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var complete = inTerm.Except(waiting).ToList();
        var decided = await db.YearPromotions
            .Where(p => p.AcademicYear == academicYear && complete.Contains(p.StudentId))
            .Select(p => p.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var ready = complete.Except(decided).ToList();
        return await db.Standings
            .Where(s => ready.Contains(s.StudentId) &&
                        (s.Status == StandingStatus.Active || s.Status == StandingStatus.Repeating || s.Status == StandingStatus.External))
            .OrderBy(s => s.StudentId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Decides by the rule; null when the department has no study plan.</summary>
    public async Task<YearPromotion?> DecideAsync(StudentStanding standing, int academicYear, CancellationToken cancellationToken) =>
        await EvaluateAsync(standing, cancellationToken) is { } evaluation
            ? Apply(standing, academicYear, evaluation.Outcome, evaluation.Failed, decidedBy: null)
            : null;

    /// <summary>
    /// A held student is decided again once nothing holds them any more (fees paid, or the policy no longer requires
    /// it); while still held nothing changes and null is returned.
    /// </summary>
    public async Task<YearPromotion?> DecideAgainAsync(StudentStanding standing, int academicYear, CancellationToken cancellationToken)
    {
        if (standing.Status != StandingStatus.OnHold)
            return null;
        var evaluation = await EvaluateAsync(standing, cancellationToken);
        return evaluation is null || evaluation.Outcome.Result == PromotionResult.Held
            ? null
            : Apply(standing, academicYear, evaluation.Outcome, evaluation.Failed, decidedBy: null);
    }

    /// <summary>The number of subjects the student still owes (failed, not passed since).</summary>
    public async Task<int> FailedSubjectsAsync(Guid studentId, CancellationToken cancellationToken) =>
        (await StudentRecord.OutstandingAsync(db, studentId, cancellationToken)).Count;

    /// <summary>The department's final level (its plan's level count), or null without a plan.</summary>
    public Task<int?> FinalLevelAsync(Guid departmentId, CancellationToken cancellationToken) =>
        db.StudyPlans.AsNoTracking()
            .Where(p => p.DepartmentId == departmentId)
            .Select(p => (int?)p.Levels)
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<PromotionEvaluation?> EvaluateAsync(StudentStanding standing, CancellationToken cancellationToken)
    {
        if (await FinalLevelAsync(standing.DepartmentId, cancellationToken) is not { } finalLevel)
            return null;

        var department = await users.GetDepartmentAsync(standing.DepartmentId, cancellationToken);
        var rules = await policies.RulesForAsync(department?.FacultyId, cancellationToken);
        var failed = await FailedSubjectsAsync(standing.StudentId, cancellationToken);
        var feesOutstanding = rules.RequireFeesPaid &&
                              await payments.GetOutstandingBalanceAsync(standing.StudentId, cancellationToken) > 0;

        var outcome = PromotionRules.Decide(
            new PromotionInput(standing.Level, finalLevel, standing.YearsInLevel, failed, feesOutstanding), rules);
        return new PromotionEvaluation(outcome, failed);
    }

    /// <summary>Applies a decision (the rule's or Student Affairs') and records it.</summary>
    public YearPromotion Apply(StudentStanding standing, int academicYear, PromotionOutcome outcome, int failed, Guid? decidedBy)
    {
        var now = clock.GetUtcNow();
        var fromLevel = standing.Level;
        standing.Apply(outcome, now);
        var record = YearPromotion.Record(
            standing.StudentId, standing.DepartmentId, academicYear, fromLevel, outcome, failed, decidedBy, now);
        db.YearPromotions.Add(record);
        return record;
    }
}
