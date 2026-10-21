using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Contracts;
using NIMI.UniPortal.Modules.Attendance.Domain.Records;

namespace NIMI.UniPortal.Modules.Attendance.Application;

/// <summary>The Attendance contract: read-only, counted in SQL.</summary>
internal sealed class AttendanceApi(IAttendanceDbContext db) : IAttendanceApi
{
    public async Task<decimal?> GetStudentRateAsync(Guid studentId, Guid termId, CancellationToken cancellationToken = default)
    {
        // Close writes a record (auto_absent at least) for every member, so the student's own rows on closed sessions
        // are exactly the sessions held for them.
        var statuses = await db.AttendanceRecords.AsNoTracking()
            .Where(r => r.StudentId == studentId)
            .Join(
                db.AttendanceSessions.AsNoTracking().Where(s => s.TermId == termId && s.ClosedAt != null),
                r => r.SessionId,
                s => s.Id,
                (r, _) => r.Status)
            .ToListAsync(cancellationToken);

        if (statuses.Count == 0)
            return null;

        var attended = statuses.Count(s => s is AttendanceStatus.Present or AttendanceStatus.Late or AttendanceStatus.ManualAudit);
        return AttendanceRate.Percentage(attended, statuses.Count);
    }
}
