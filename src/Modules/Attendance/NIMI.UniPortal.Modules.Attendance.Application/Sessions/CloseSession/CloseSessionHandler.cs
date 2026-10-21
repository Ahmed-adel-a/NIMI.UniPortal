using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Common;
using NIMI.UniPortal.Modules.Attendance.Domain.Records;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.CloseSession;

/// <summary>
/// P6.5 — closes the session and fills the gaps with <c>absent</c> / <c>auto_absent</c> records for every confirmed
/// member who has none (R8). A second close changes nothing and reports the same summary.
/// </summary>
internal sealed class CloseSessionHandler(
    IAttendanceDbContext db,
    IAcademicLifecycle lifecycle,
    ISchedulingApi scheduling,
    TimeProvider clock) : IRequestHandler<CloseSessionCommand, CloseSummaryDto>
{
    public async Task<CloseSummaryDto> Handle(CloseSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await db.AttendanceSessions
            .SingleOrDefaultAsync(s => s.Id == command.SessionId, cancellationToken)
            ?? throw new NotFoundException("Attendance session not found.");

        await AttendanceAccess.EnsureTeachingAsync(scheduling, session, command.CallerId, cancellationToken);

        var records = await db.AttendanceRecords
            .Where(r => r.SessionId == session.Id)
            .ToListAsync(cancellationToken);

        if (session.IsOpen)
        {
            var now = clock.GetUtcNow();
            session.Close(now);

            var recorded = records.Select(r => r.StudentId).ToHashSet();
            var members = await lifecycle.GetStudentsInGroupAsync(session.SubjectGroupId, cancellationToken);
            foreach (var studentId in members.Where(id => !recorded.Contains(id)))
            {
                var absent = AttendanceRecord.AutoAbsent(session.Id, studentId, now);
                db.AttendanceRecords.Add(absent);
                records.Add(absent);
            }
        }

        return new CloseSummaryDto(
            session.Id,
            session.ClosedAt!.Value,
            Present: records.Count(r => r.Status == AttendanceStatus.Present),
            Late: records.Count(r => r.Status == AttendanceStatus.Late),
            ManualAudit: records.Count(r => r.Status == AttendanceStatus.ManualAudit),
            Absent: records.Count(r => r.Status == AttendanceStatus.Absent));
    }
}
