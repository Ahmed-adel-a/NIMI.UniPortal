using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Common;
using NIMI.UniPortal.Modules.Attendance.Domain.Records;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.LiveRoster;

internal sealed class GetLiveRosterHandler(
    IAttendanceDbContext db,
    IAcademicLifecycle lifecycle,
    ISchedulingApi scheduling,
    IUsersApi users) : IRequestHandler<GetLiveRosterQuery, LiveRosterDto>
{
    private const string NotYet = "not_yet";

    public async Task<LiveRosterDto> Handle(GetLiveRosterQuery query, CancellationToken cancellationToken)
    {
        var session = await db.AttendanceSessions.AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == query.SessionId, cancellationToken)
            ?? throw new NotFoundException("Attendance session not found.");

        await AttendanceAccess.EnsureTeachingAsync(scheduling, session, query.CallerId, cancellationToken);

        var statuses = await db.AttendanceRecords.AsNoTracking()
            .Where(r => r.SessionId == session.Id)
            .ToDictionaryAsync(r => r.StudentId, r => r.Status, cancellationToken);

        var members = await GroupMembers.LoadAsync(lifecycle, users, session.SubjectGroupId, cancellationToken);
        var items = members
            .Select(m => new LiveRosterItemDto(
                m.StudentId,
                m.FullName,
                m.StudentCode,
                statuses.TryGetValue(m.StudentId, out var status) ? AttendanceWire.Status(status) : NotYet))
            .ToList();

        var counts = new LiveRosterCountsDto(
            Present: items.Count(i => i.Status == AttendanceWire.Status(AttendanceStatus.Present)),
            Late: items.Count(i => i.Status == AttendanceWire.Status(AttendanceStatus.Late)),
            ManualAudit: items.Count(i => i.Status == AttendanceWire.Status(AttendanceStatus.ManualAudit)),
            Absent: items.Count(i => i.Status == AttendanceWire.Status(AttendanceStatus.Absent)),
            NotYet: items.Count(i => i.Status == NotYet));

        return new LiveRosterDto(session.Id, session.IsOpen, items, counts);
    }
}
