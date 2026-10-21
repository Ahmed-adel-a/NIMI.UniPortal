using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Grades.Application.Abstractions;
using NIMI.UniPortal.Modules.Grades.Domain;

namespace NIMI.UniPortal.Modules.Grades.Application.Common;

/// <summary>
/// The grade rows of a group's roster students for the group's subject and term (one per student — the unique key
/// is student, subject, term), keyed by student.
/// </summary>
internal static class GroupGrades
{
    public static async Task<Dictionary<Guid, FinalGrade>> LoadTrackedAsync(
        IGradesDbContext db, GradedGroup graded, IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken) =>
        await Query(db.FinalGrades, graded, studentIds).ToDictionaryAsync(g => g.StudentId, cancellationToken);

    public static async Task<Dictionary<Guid, FinalGrade>> LoadAsync(
        IGradesDbContext db, GradedGroup graded, IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken) =>
        await Query(db.FinalGrades.AsNoTracking(), graded, studentIds).ToDictionaryAsync(g => g.StudentId, cancellationToken);

    private static IQueryable<FinalGrade> Query(IQueryable<FinalGrade> grades, GradedGroup graded, IReadOnlyCollection<Guid> studentIds) =>
        grades.Where(g => g.SubjectId == graded.Subject.Id &&
                          g.TermId == graded.Group.TermId &&
                          studentIds.Contains(g.StudentId));
}
