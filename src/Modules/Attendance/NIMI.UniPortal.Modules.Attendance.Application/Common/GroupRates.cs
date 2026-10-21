using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Domain.Records;

namespace NIMI.UniPortal.Modules.Attendance.Application.Common;

/// <summary>
/// Rule R9 in one place: attended (present, late, manual_audit) over the group's <b>closed</b> sessions. Counting
/// happens in SQL; open sessions never count yet.
/// </summary>
internal static class GroupRates
{
    public static async Task<GroupAttendance> LoadAsync(
        IAttendanceDbContext db, Guid subjectGroupId, CancellationToken cancellationToken)
    {
        var closedSessionIds = await db.AttendanceSessions.AsNoTracking()
            .Where(s => s.SubjectGroupId == subjectGroupId && s.ClosedAt != null)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        if (closedSessionIds.Count == 0)
            return new GroupAttendance(0, new Dictionary<Guid, int>());

        var attended = await db.AttendanceRecords.AsNoTracking()
            .Where(r => closedSessionIds.Contains(r.SessionId) &&
                        (r.Status == AttendanceStatus.Present ||
                         r.Status == AttendanceStatus.Late ||
                         r.Status == AttendanceStatus.ManualAudit))
            .GroupBy(r => r.StudentId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StudentId, x => x.Count, cancellationToken);

        return new GroupAttendance(closedSessionIds.Count, attended);
    }
}
