using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Enrollment;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Lifecycle;

/// <summary>
/// Decision LIFECYCLE: the SemesterBased implementation, composed by the reference host. Rosters are term
/// enrollments; every enrollment except a withdrawn one is a member (they are generated, never pending).
/// </summary>
internal sealed class AcademicLifecycleApi(ISemesterDbContext db, ISchedulingApi scheduling, IUsersApi users) : IAcademicLifecycle
{
    public async Task<IReadOnlyList<Guid>> GetStudentsInGroupAsync(Guid subjectGroupId, CancellationToken cancellationToken = default) =>
        await db.TermEnrollments.AsNoTracking()
            .Where(e => e.SubjectGroupId == subjectGroupId && e.Status != EnrollmentStatus.Withdrawn)
            .Select(e => e.StudentId)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetGroupsForStudentAsync(
        Guid studentId, Guid termId, CancellationToken cancellationToken = default) =>
        await db.TermEnrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId && e.TermId == termId && e.Status != EnrollmentStatus.Withdrawn)
            .Select(e => e.SubjectGroupId)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);

    public Task<bool> IsStudentInGroupAsync(Guid studentId, Guid subjectGroupId, CancellationToken cancellationToken = default) =>
        db.TermEnrollments.AsNoTracking().AnyAsync(
            e => e.StudentId == studentId && e.SubjectGroupId == subjectGroupId && e.Status != EnrollmentStatus.Withdrawn,
            cancellationToken);

    public async Task<StudentProgress> GetProgressAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var results = await db.TermEnrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId &&
                        (e.Status == EnrollmentStatus.Passed || e.Status == EnrollmentStatus.Failed))
            .Select(e => new { e.SubjectId, e.Status, e.CreditHours, e.GradePoints, e.ResultAt })
            .ToListAsync(cancellationToken);

        // The latest result per subject counts (a re-sit replaces the failure); credits come from passed subjects.
        var latest = results
            .GroupBy(r => r.SubjectId)
            .Select(g => g.OrderByDescending(r => r.ResultAt).First())
            .ToList();
        var passedSubjects = results.Where(r => r.Status == EnrollmentStatus.Passed).Select(r => r.SubjectId).ToHashSet();
        var creditsEarned = latest.Where(r => passedSubjects.Contains(r.SubjectId)).Sum(r => r.CreditHours);
        var weighted = latest.Where(r => r.CreditHours > 0 && r.GradePoints is not null).ToList();
        decimal? cumulativeGpa = weighted.Count == 0
            ? null
            : Math.Round(weighted.Sum(r => r.GradePoints!.Value * r.CreditHours) / weighted.Sum(r => r.CreditHours), 2);

        var term = await scheduling.GetCurrentTermAsync(cancellationToken);
        var creditsInProgress = term is null
            ? 0
            : await db.TermEnrollments.AsNoTracking()
                .Where(e => e.StudentId == studentId && e.TermId == term.Id && e.Status == EnrollmentStatus.Enrolled)
                .SumAsync(e => (int?)e.CreditHours, cancellationToken) ?? 0;

        var requirements = await scheduling.GetGraduationRequirementSubjectsAsync(cancellationToken);
        var outstanding = requirements.Where(s => !passedSubjects.Contains(s.Id)).Select(s => s.Id).ToList();

        return new StudentProgress(studentId, creditsEarned, creditsInProgress, cumulativeGpa, outstanding);
    }

    public async Task<int> CountConfirmedRegistrationsAsync(
        Guid termId, IReadOnlyCollection<Guid>? studentIds, CancellationToken cancellationToken = default)
    {
        var enrollments = db.TermEnrollments.AsNoTracking()
            .Where(e => e.TermId == termId && e.Status != EnrollmentStatus.Withdrawn);
        if (studentIds is not null)
        {
            var ids = studentIds.ToList();
            enrollments = enrollments.Where(e => ids.Contains(e.StudentId));
        }

        return await enrollments.CountAsync(cancellationToken);
    }

    /// <summary>Students in their department's final level who could not graduate: repeating, external or held.</summary>
    public async Task<int> CountGraduationBlockedAsync(Guid? facultyId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        var scope = (await users.GetActiveStudentIdsAsync(facultyId, departmentId, cancellationToken)).ToList();
        var finalLevels = await db.StudyPlans.AsNoTracking().ToDictionaryAsync(p => p.DepartmentId, p => p.Levels, cancellationToken);
        var stuck = await db.Standings.AsNoTracking()
            .Where(s => scope.Contains(s.StudentId) &&
                        (s.Status == StandingStatus.Repeating || s.Status == StandingStatus.External || s.Status == StandingStatus.OnHold))
            .Select(s => new { s.DepartmentId, s.Level })
            .ToListAsync(cancellationToken);

        return stuck.Count(s => finalLevels.TryGetValue(s.DepartmentId, out var final) && s.Level >= final);
    }
}
