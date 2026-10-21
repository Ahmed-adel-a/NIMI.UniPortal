using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Materials.Application.Common;

/// <summary>
/// Who may see or manage a subject's materials (D1, R1, R2): teaching staff of the subject in the term, students
/// registered in one of its groups (rosters through <see cref="IAcademicLifecycle"/>), and Student Affairs.
/// </summary>
internal sealed class CourseAccess(ISchedulingApi scheduling, IAcademicLifecycle lifecycle)
{
    public async Task<TermRef> ActiveTermAsync(CancellationToken cancellationToken) =>
        await scheduling.GetCurrentTermAsync(cancellationToken)
        ?? throw ValidationErrors.For("subjectId", "There is no active term.");

    /// <summary>An active assignment for the subject in the term — for the whole subject or any of its groups.</summary>
    public async Task<bool> TeachesSubjectAsync(Guid staffId, Guid subjectId, Guid termId, CancellationToken cancellationToken)
    {
        if (await scheduling.IsTeachingAsync(staffId, subjectId, termId, null, cancellationToken))
            return true;

        foreach (var groupId in await scheduling.GetGroupIdsTaughtByAsync(staffId, termId, cancellationToken))
        {
            if ((await scheduling.GetGroupAsync(groupId, cancellationToken))?.SubjectId == subjectId)
                return true;
        }

        return false;
    }

    /// <summary>The student's own groups of this subject in the term (empty = not registered in it).</summary>
    public async Task<IReadOnlyList<Guid>> StudentGroupsAsync(Guid studentId, Guid subjectId, Guid termId, CancellationToken cancellationToken)
    {
        var groups = new List<Guid>();
        foreach (var groupId in await lifecycle.GetGroupsForStudentAsync(studentId, termId, cancellationToken))
        {
            if ((await scheduling.GetGroupAsync(groupId, cancellationToken))?.SubjectId == subjectId)
                groups.Add(groupId);
        }

        return groups;
    }

    /// <summary>Students a new item is for: one group's members, or every group of the subject in the term.</summary>
    public async Task<IReadOnlyList<Guid>> AudienceAsync(Guid subjectId, Guid termId, Guid? subjectGroupId, CancellationToken cancellationToken)
    {
        IEnumerable<Guid> groupIds = subjectGroupId is { } single
            ? [single]
            : (await scheduling.GetOfferingsAsync(termId, cancellationToken))
                .Where(o => o.Subject.Id == subjectId)
                .Select(o => o.Group.Id);

        var students = new HashSet<Guid>();
        foreach (var groupId in groupIds)
            students.UnionWith(await lifecycle.GetStudentsInGroupAsync(groupId, cancellationToken));

        return [.. students];
    }
}
