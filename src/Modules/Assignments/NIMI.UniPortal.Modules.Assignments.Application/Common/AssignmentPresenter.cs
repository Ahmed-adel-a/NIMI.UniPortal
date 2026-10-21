using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Assignments.Application.Abstractions;
using NIMI.UniPortal.Modules.Assignments.Domain;
using NIMI.UniPortal.Modules.Scheduling.Contracts;

namespace NIMI.UniPortal.Modules.Assignments.Application.Common;

/// <summary>Builds <see cref="AssignmentDto"/>s: subject codes, submission counts (one query) and the student's own status.</summary>
internal sealed class AssignmentPresenter(IAssignmentsDbContext db, ISchedulingApi scheduling, TimeProvider clock)
{
    public async Task<IReadOnlyList<AssignmentDto>> PresentAsync(
        IReadOnlyList<Assignment> assignments, Viewer viewer, bool staffView, CancellationToken cancellationToken)
    {
        var ids = assignments.Select(a => a.Id).ToList();

        Dictionary<Guid, int> counts = [];
        if (staffView && ids.Count > 0)
        {
            counts = await db.Submissions.AsNoTracking()
                .Where(s => ids.Contains(s.AssignmentId))
                .GroupBy(s => s.AssignmentId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        }

        Dictionary<Guid, SubmissionStatus> mine = [];
        if (viewer.IsStudent && ids.Count > 0)
        {
            mine = await db.Submissions.AsNoTracking()
                .Where(s => s.StudentId == viewer.UserId && ids.Contains(s.AssignmentId))
                .ToDictionaryAsync(s => s.AssignmentId, s => s.Status, cancellationToken);
        }

        var codes = new Dictionary<Guid, string>();
        foreach (var subjectId in assignments.Select(a => a.SubjectId).Distinct())
            codes[subjectId] = (await scheduling.GetSubjectAsync(subjectId, cancellationToken))?.Code ?? string.Empty;

        var now = clock.GetUtcNow();
        return [.. assignments.Select(a => new AssignmentDto(
            a.Id,
            a.SubjectId,
            codes[a.SubjectId],
            a.SubjectGroupId,
            a.Title,
            a.Instructions,
            a.DueAt,
            a.MaxGrade,
            a.AllowedExtensionList,
            a.MaxFileMb,
            a.CreatedBy,
            a.CreatedAt,
            a.UpdatedAt,
            a.IsOpenAt(now),
            Math.Max(0L, (long)(a.DueAt - now).TotalSeconds),
            staffView ? counts.GetValueOrDefault(a.Id) : null,
            viewer.IsStudent && mine.TryGetValue(a.Id, out var status) ? status : null))];
    }

    public async Task<AssignmentDto> PresentAsync(Assignment assignment, Viewer viewer, bool staffView, CancellationToken cancellationToken) =>
        (await PresentAsync([assignment], viewer, staffView, cancellationToken))[0];
}
