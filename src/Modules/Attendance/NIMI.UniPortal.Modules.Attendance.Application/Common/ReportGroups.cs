using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Contracts;

namespace NIMI.UniPortal.Modules.Attendance.Application.Common;

/// <summary>
/// The groups a term-wide attendance report covers (at-risk P6.7, finals eligibility P10.5): one named group (Student
/// Affairs or its teacher, else 403), otherwise every group with sessions in the term for Student Affairs and the
/// caller's own groups for a professor. The term defaults to the active one.
/// </summary>
internal static class ReportGroups
{
    public static async Task<IReadOnlyList<GroupRef>> LoadAsync(
        IAttendanceDbContext db,
        ISchedulingApi scheduling,
        Guid callerId,
        bool isAdmin,
        Guid? termId,
        Guid? groupId,
        CancellationToken cancellationToken)
    {
        if (groupId is { } id)
            return [await GroupAccess.EnsureCanSeeAsync(scheduling, id, callerId, isAdmin, cancellationToken)];

        var term = termId ?? (await scheduling.GetCurrentTermAsync(cancellationToken))?.Id;
        if (term is null)
            return [];

        var groupIds = isAdmin
            ? await db.AttendanceSessions.AsNoTracking()
                .Where(s => s.TermId == term)
                .Select(s => s.SubjectGroupId)
                .Distinct()
                .ToListAsync(cancellationToken)
            : [.. await scheduling.GetGroupIdsTaughtByAsync(callerId, term.Value, cancellationToken)];

        var groups = new List<GroupRef>(groupIds.Count);
        foreach (var groupIdInTerm in groupIds)
        {
            if (await scheduling.GetGroupAsync(groupIdInTerm, cancellationToken) is { } group)
                groups.Add(group);
        }

        return groups;
    }
}
