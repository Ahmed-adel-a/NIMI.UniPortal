using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Reports.Eligibility;
using NIMI.UniPortal.Modules.Attendance.Contracts;
using NIMI.UniPortal.Modules.Attendance.Domain.Records;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Attendance.Application;

/// <summary>The Attendance contract: read-only, counted in SQL.</summary>
internal sealed class AttendanceApi(IAttendanceDbContext db, ISender sender) : IAttendanceApi
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

    public async Task<AttendanceTermSummary> GetTermSummaryAsync(
        Guid termId, IReadOnlyCollection<Guid>? studentIds, CancellationToken cancellationToken = default)
    {
        var scope = studentIds?.ToHashSet();
        var ids = studentIds?.ToList();
        var records = db.AttendanceRecords.AsNoTracking()
            .Join(
                db.AttendanceSessions.AsNoTracking().Where(s => s.TermId == termId && s.ClosedAt != null),
                r => r.SessionId,
                s => s.Id,
                (r, _) => r);
        if (ids is not null)
            records = records.Where(r => ids.Contains(r.StudentId));

        var held = await records.CountAsync(cancellationToken);
        var attended = await records.CountAsync(
            r => r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late || r.Status == AttendanceStatus.ManualAudit,
            cancellationToken);

        // Barring is the finals-eligibility rule (P10.5): read it from there, Student Affairs view, every page.
        var barred = new HashSet<Guid>();
        for (var page = 1; ; page++)
        {
            var result = await sender.Send(
                new GetFinalsEligibilityQuery(Guid.Empty, true, false, termId, null, null, true, new PageRequest(page, PageRequest.MaxPageSize)),
                cancellationToken);
            barred.UnionWith(result.Items.Select(i => i.StudentId).Where(id => scope is null || scope.Contains(id)));
            if (page >= result.TotalPages)
                break;
        }

        return new AttendanceTermSummary(held == 0 ? null : AttendanceRate.Percentage(attended, held), barred.Count);
    }
}
