using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Enrollment;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Results;

/// <summary>
/// What a student still owes: subjects with a failed result and no pass since (any term, any level). These are the
/// carried subjects (مواد تخلف) and what a repeating or external student takes again.
/// </summary>
internal static class StudentRecord
{
    public static async Task<Dictionary<Guid, List<OutstandingSubject>>> OutstandingAsync(
        ISemesterDbContext db, IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken)
    {
        var ids = studentIds.ToList();
        var results = await db.TermEnrollments.AsNoTracking()
            .Where(e => ids.Contains(e.StudentId) &&
                        (e.Status == EnrollmentStatus.Passed || e.Status == EnrollmentStatus.Failed))
            .Select(e => new { e.StudentId, e.SubjectId, e.Level, e.Status })
            .ToListAsync(cancellationToken);

        var outstanding = ids.ToDictionary(id => id, _ => new List<OutstandingSubject>());
        foreach (var subject in results.GroupBy(r => (r.StudentId, r.SubjectId)))
        {
            if (subject.Any(r => r.Status == EnrollmentStatus.Passed))
                continue;
            outstanding[subject.Key.StudentId].Add(new OutstandingSubject(subject.Key.SubjectId, subject.Min(r => r.Level)));
        }

        return outstanding;
    }

    public static async Task<List<OutstandingSubject>> OutstandingAsync(
        ISemesterDbContext db, Guid studentId, CancellationToken cancellationToken) =>
        (await OutstandingAsync(db, [studentId], cancellationToken))[studentId];
}
