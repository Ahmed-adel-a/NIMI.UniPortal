using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Common;
using NIMI.UniPortal.Modules.Attendance.Domain.Records;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.History;

/// <summary>
/// P6.6 — the student's own records. Every closed session of the student's groups has a record for them (close adds
/// <c>auto_absent</c>), so held/attended per subject come straight from their own rows.
/// </summary>
internal sealed class GetAttendanceHistoryHandler(IAttendanceDbContext db, ISchedulingApi scheduling)
    : IRequestHandler<GetAttendanceHistoryQuery, AttendanceHistoryDto>
{
    public async Task<AttendanceHistoryDto> Handle(GetAttendanceHistoryQuery query, CancellationToken cancellationToken)
    {
        if (!query.IsStudent)
            throw new ForbiddenException("Only students have an attendance history.");

        var rows = await db.AttendanceRecords.AsNoTracking()
            .Where(r => r.StudentId == query.StudentId)
            .Join(
                db.AttendanceSessions.AsNoTracking(),
                r => r.SessionId,
                s => s.Id,
                (r, s) => new
                {
                    s.Id,
                    s.SessionDate,
                    s.OpenedAt,
                    s.ClosedAt,
                    s.SubjectId,
                    s.TermId,
                    r.Status,
                    r.Method,
                    r.CheckedInAt,
                })
            .Where(x => query.SubjectId == null || x.SubjectId == query.SubjectId)
            .Where(x => query.TermId == null || x.TermId == query.TermId)
            .ToListAsync(cancellationToken);

        var codes = new Dictionary<Guid, string>();
        foreach (var subjectId in rows.Select(x => x.SubjectId).Distinct())
            codes[subjectId] = (await scheduling.GetSubjectAsync(subjectId, cancellationToken))?.Code ?? string.Empty;

        var ordered = rows
            .OrderByDescending(x => x.SessionDate)
            .ThenByDescending(x => x.OpenedAt)
            .ToList();
        var items = ordered
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .Select(x => new AttendanceHistoryItemDto(
                x.Id,
                x.SessionDate,
                codes[x.SubjectId],
                AttendanceWire.Status(x.Status),
                AttendanceWire.Method(x.Method),
                x.CheckedInAt))
            .ToList();

        var subjects = rows
            .Where(x => x.ClosedAt != null)
            .GroupBy(x => x.SubjectId)
            .Select(g =>
            {
                var held = g.Count();
                var attended = g.Count(x => x.Status is AttendanceStatus.Present or AttendanceStatus.Late or AttendanceStatus.ManualAudit);
                return new SubjectRateDto(g.Key, codes[g.Key], attended, held, AttendanceRate.Percentage(attended, held));
            })
            .OrderBy(s => s.SubjectCode, StringComparer.Ordinal)
            .ToList();

        var totalPages = (int)Math.Ceiling(rows.Count / (double)query.Paging.PageSize);
        return new AttendanceHistoryDto(items, query.Paging.Page, query.Paging.PageSize, rows.Count, totalPages, subjects);
    }
}
