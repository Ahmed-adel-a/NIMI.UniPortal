using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Results;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;
using NIMI.UniPortal.Modules.Scheduling.Contracts;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Lifecycle;

/// <summary>
/// Decision LIFECYCLE: the one implementation composed by Host.CairoUniversity. Rosters are course registrations,
/// and only confirmed members count (R11: registered/completed/failed — never pending_payment or dropped).
/// </summary>
internal sealed class AcademicLifecycleApi(ICreditHoursDbContext db, ISchedulingApi scheduling) : IAcademicLifecycle
{
    // The confirmed-member predicate is written out in every query: EF Core can translate the enum comparisons,
    // but it cannot translate a call to a C# helper method inside a Where clause.
    public async Task<IReadOnlyList<Guid>> GetStudentsInGroupAsync(
        Guid subjectGroupId, CancellationToken cancellationToken = default) =>
        await db.CourseRegistrations.AsNoTracking()
            .Where(r => r.SubjectGroupId == subjectGroupId &&
                        (r.Status == RegistrationStatus.Registered ||
                         r.Status == RegistrationStatus.Completed ||
                         r.Status == RegistrationStatus.Failed))
            .Select(r => r.StudentId)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetGroupsForStudentAsync(
        Guid studentId, Guid termId, CancellationToken cancellationToken = default) =>
        await db.CourseRegistrations.AsNoTracking()
            .Where(r => r.StudentId == studentId &&
                        r.TermId == termId &&
                        (r.Status == RegistrationStatus.Registered ||
                         r.Status == RegistrationStatus.Completed ||
                         r.Status == RegistrationStatus.Failed))
            .Select(r => r.SubjectGroupId)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);

    public Task<bool> IsStudentInGroupAsync(
        Guid studentId, Guid subjectGroupId, CancellationToken cancellationToken = default) =>
        db.CourseRegistrations.AsNoTracking()
            .AnyAsync(
                r => r.StudentId == studentId &&
                     r.SubjectGroupId == subjectGroupId &&
                     (r.Status == RegistrationStatus.Registered ||
                      r.Status == RegistrationStatus.Completed ||
                      r.Status == RegistrationStatus.Failed),
                cancellationToken);

    public async Task<StudentProgress> GetProgressAsync(
        Guid studentId, CancellationToken cancellationToken = default)
    {
        // P9.3b: the ledger is written from GradesPublished; GPA is the latest term snapshot's cumulative GPA.
        var passed = await db.CreditLedger.AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Passed)
            .Select(e => new PassedSubject(e.StudentId, e.SubjectId, e.CreditHours, e.RecordedAt))
            .ToListAsync(cancellationToken);

        var creditsEarned = EarnedCredits.Of(passed);
        var passedSubjectIds = passed.Select(p => p.SubjectId).ToHashSet();
        var cumulativeGpa = await db.GpaSnapshots.AsNoTracking()
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.TermStartsOn)
            .Select(s => s.CumulativeGpa)
            .FirstOrDefaultAsync(cancellationToken);

        var term = await scheduling.GetCurrentTermAsync(cancellationToken);
        var creditsInProgress = term is null
            ? 0
            : await db.CourseRegistrations.AsNoTracking()
                .Where(r => r.StudentId == studentId &&
                            r.TermId == term.Id &&
                            r.Status == RegistrationStatus.Registered)
                .SumAsync(r => (int?)r.CreditHours, cancellationToken) ?? 0;

        var requirements = await scheduling.GetGraduationRequirementSubjectsAsync(cancellationToken);
        var outstanding = requirements
            .Where(s => !passedSubjectIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToList();

        return new StudentProgress(studentId, creditsEarned, creditsInProgress, cumulativeGpa, outstanding);
    }
}
