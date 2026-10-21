using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Grades.Application.Abstractions;
using NIMI.UniPortal.Modules.Grades.Contracts;
using NIMI.UniPortal.Modules.Grades.Domain;

namespace NIMI.UniPortal.Modules.Grades.Application;

/// <summary>The Grades contract: read-only, counted in SQL.</summary>
internal sealed class GradesApi(IGradesDbContext db) : IGradesApi
{
    public async Task<GradeBacklog> GetBacklogAsync(
        Guid termId, IReadOnlyCollection<Guid>? studentIds, CancellationToken cancellationToken = default)
    {
        var grades = db.FinalGrades.AsNoTracking().Where(g => g.TermId == termId);
        if (studentIds is not null)
        {
            var ids = studentIds.ToList();
            grades = grades.Where(g => ids.Contains(g.StudentId));
        }

        var drafts = await grades.CountAsync(g => g.Status == GradeStatus.Draft, cancellationToken);
        var confirmed = await grades.CountAsync(g => g.Status == GradeStatus.Confirmed, cancellationToken);
        return new GradeBacklog(drafts, confirmed);
    }
}
