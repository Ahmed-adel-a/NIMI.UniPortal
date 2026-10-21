using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Common;
using NIMI.UniPortal.Modules.Attendance.Domain.Records;
using NIMI.UniPortal.Modules.Attendance.Domain.Sessions;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.CloseSession;

/// <summary>
/// P6.5 + P6.7 — closes the session and fills the gaps with <c>absent</c> / <c>auto_absent</c> records for every
/// confirmed member who has none (R8). Then each member's rate is recomputed: a student newly below the effective
/// threshold is warned once (R10) and the warning is forgotten when they are back at or above it, so the next drop
/// warns again. A second close changes nothing and reports the same summary.
/// </summary>
internal sealed class CloseSessionHandler(
    IAttendanceDbContext db,
    IAcademicLifecycle lifecycle,
    ISchedulingApi scheduling,
    IUsersApi users,
    IPlatformSettingsApi platform,
    IEventCollector events,
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
            // PostgreSQL keeps microseconds; trimming the 100 ns ticks makes the first close report exactly the
            // closedAt every later read (and a repeated close) returns.
            var exact = clock.GetUtcNow();
            var now = exact.AddTicks(-(exact.Ticks % TimeSpan.TicksPerMicrosecond));
            session.Close(now);

            var recorded = records.Select(r => r.StudentId).ToHashSet();
            var members = await lifecycle.GetStudentsInGroupAsync(session.SubjectGroupId, cancellationToken);
            foreach (var studentId in members.Where(id => !recorded.Contains(id)))
            {
                var absent = AttendanceRecord.AutoAbsent(session.Id, studentId, now);
                db.AttendanceRecords.Add(absent);
                records.Add(absent);
            }

            await WarnNewlyBelowThresholdAsync(session, members, records, now, cancellationToken);
        }

        return new CloseSummaryDto(
            session.Id,
            session.ClosedAt!.Value,
            Present: records.Count(r => r.Status == AttendanceStatus.Present),
            Late: records.Count(r => r.Status == AttendanceStatus.Late),
            ManualAudit: records.Count(r => r.Status == AttendanceStatus.ManualAudit),
            Absent: records.Count(r => r.Status == AttendanceStatus.Absent));
    }

    private async Task WarnNewlyBelowThresholdAsync(
        AttendanceSession session,
        IReadOnlyList<Guid> members,
        IReadOnlyList<AttendanceRecord> currentRecords,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (members.Count == 0)
            return;

        var subject = await scheduling.GetSubjectAsync(session.SubjectId, cancellationToken)
            ?? throw new NotFoundException("The session's subject was not found.");
        var settings = await AttendanceSettings.ForDepartmentAsync(users, platform, subject.DepartmentId, cancellationToken);

        // The earlier closed sessions come from the database; this one is closed in memory and counted on top.
        var earlier = await GroupRates.LoadAsync(db, session.SubjectGroupId, cancellationToken);
        var attendedNow = currentRecords.Where(r => r.Attended).Select(r => r.StudentId).ToHashSet();
        var warned = await db.AttendanceWarnings
            .Where(w => w.SubjectGroupId == session.SubjectGroupId)
            .ToDictionaryAsync(w => w.StudentId, cancellationToken);

        var newlyBelow = new List<Guid>();
        foreach (var studentId in members)
        {
            var attended = earlier.AttendedBy(studentId) + (attendedNow.Contains(studentId) ? 1 : 0);
            var percentage = AttendanceRate.Percentage(attended, earlier.Held + 1);
            var below = percentage < settings.AttendanceThreshold;

            if (below && !warned.ContainsKey(studentId))
            {
                db.AttendanceWarnings.Add(AttendanceWarning.Create(studentId, session.SubjectGroupId, now));
                newlyBelow.Add(studentId);
            }
            else if (!below && warned.TryGetValue(studentId, out var warning))
            {
                db.AttendanceWarnings.Remove(warning);
            }
        }

        if (newlyBelow.Count == 0)
            return;

        events.Add(new NotificationRequested(
            Guid.CreateVersion7(),
            now,
            newlyBelow,
            "attendance",
            "Attendance warning",
            "تحذير الحضور",
            $"Your attendance in {subject.Code} is below the required {settings.AttendanceThreshold:0.##}%.",
            $"نسبة حضورك في مادة {subject.Code} أقل من النسبة المطلوبة {settings.AttendanceThreshold:0.##}٪.",
            "/attendance"));
    }
}
