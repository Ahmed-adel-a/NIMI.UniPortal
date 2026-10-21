using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Common;
using NIMI.UniPortal.Modules.Attendance.Domain.Records;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.Export;

/// <summary>
/// P10.4 — closed sessions only (rule R9, like every rate): close writes a record for every member, so each cell is the
/// member's status in that session; totals are attended (present, late, manual_audit) over held.
/// </summary>
internal sealed class GetAttendanceSheetHandler(
    IAttendanceDbContext db, IAcademicLifecycle lifecycle, ISchedulingApi scheduling, IUsersApi users)
    : IRequestHandler<GetAttendanceSheetQuery, AttendanceSheetDto>
{
    public async Task<AttendanceSheetDto> Handle(GetAttendanceSheetQuery query, CancellationToken cancellationToken)
    {
        var group = await GroupAccess.EnsureCanSeeAsync(scheduling, query.GroupId, query.CallerId, query.IsAdmin, cancellationToken);
        var subject = await scheduling.GetSubjectAsync(group.SubjectId, cancellationToken);

        var sessions = await db.AttendanceSessions.AsNoTracking()
            .Where(s => s.SubjectGroupId == group.Id && s.ClosedAt != null)
            .OrderBy(s => s.SessionDate)
            .ThenBy(s => s.OpenedAt)
            .Select(s => new { s.Id, s.SessionDate })
            .ToListAsync(cancellationToken);
        var sessionIds = sessions.Select(s => s.Id).ToList();

        var records = await db.AttendanceRecords.AsNoTracking()
            .Where(r => sessionIds.Contains(r.SessionId))
            .Select(r => new { r.SessionId, r.StudentId, r.Status })
            .ToListAsync(cancellationToken);
        var statusOf = records.ToDictionary(r => (r.SessionId, r.StudentId), r => r.Status);

        var members = await GroupMembers.LoadAsync(lifecycle, users, group.Id, cancellationToken);
        var rows = members.Select(m =>
        {
            var statuses = sessions
                .Select(s => statusOf.TryGetValue((s.Id, m.StudentId), out var status) ? status : (AttendanceStatus?)null)
                .ToList();
            var attended = statuses.Count(s => s is AttendanceStatus.Present or AttendanceStatus.Late or AttendanceStatus.ManualAudit);
            return new AttendanceSheetRowDto(
                m.StudentId,
                m.FullName,
                m.StudentCode,
                attended,
                sessions.Count,
                AttendanceRate.Percentage(attended, sessions.Count),
                [.. statuses.Select(s => s is { } value ? AttendanceWire.Status(value) : null)]);
        }).ToList();

        return new AttendanceSheetDto(
            group.Id, subject?.Code ?? string.Empty, group.GroupNumber, [.. sessions.Select(s => s.SessionDate)], rows);
    }
}
