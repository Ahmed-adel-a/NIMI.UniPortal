using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Common;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.AtRisk;

/// <summary>
/// P6.7 — for each visible group with at least one closed session, the members whose rate is below the effective
/// threshold of the subject's faculty. Ordered by subject code, group number, then student code.
/// </summary>
internal sealed class GetAtRiskHandler(
    IAttendanceDbContext db,
    IAcademicLifecycle lifecycle,
    ISchedulingApi scheduling,
    IUsersApi users,
    IPlatformSettingsApi platform) : IRequestHandler<GetAtRiskQuery, PagedResult<AtRiskItemDto>>
{
    public async Task<PagedResult<AtRiskItemDto>> Handle(GetAtRiskQuery query, CancellationToken cancellationToken)
    {
        if (!query.IsAdmin && !query.IsProfessor)
            throw new ForbiddenException("Only teaching staff and Student Affairs see the at-risk list.");

        var groups = await ReportGroups.LoadAsync(
            db, scheduling, query.CallerId, query.IsAdmin, query.TermId, query.GroupId, cancellationToken);

        var items = new List<AtRiskItemDto>();
        foreach (var group in groups)
        {
            var rates = await GroupRates.LoadAsync(db, group.Id, cancellationToken);
            if (rates.Held == 0)
                continue;

            var subject = await scheduling.GetSubjectAsync(group.SubjectId, cancellationToken);
            if (subject is null)
                continue;

            var settings = await AttendanceSettings.ForDepartmentAsync(users, platform, subject.DepartmentId, cancellationToken);
            var members = await GroupMembers.LoadAsync(lifecycle, users, group.Id, cancellationToken);
            items.AddRange(members
                .Select(m => (Member: m, Percentage: rates.PercentageOf(m.StudentId)))
                .Where(x => x.Percentage < settings.AttendanceThreshold)
                .Select(x => new AtRiskItemDto(
                    x.Member.StudentId,
                    x.Member.FullName,
                    x.Member.StudentCode,
                    group.Id,
                    subject.Code,
                    group.GroupNumber,
                    x.Percentage,
                    settings.AttendanceThreshold)));
        }

        var ordered = items
            .OrderBy(i => i.SubjectCode, StringComparer.Ordinal)
            .ThenBy(i => i.GroupNumber)
            .ThenBy(i => i.StudentCode, StringComparer.Ordinal)
            .ToList();

        return new PagedResult<AtRiskItemDto>(
            [.. ordered.Skip(query.Paging.Skip).Take(query.Paging.PageSize)],
            query.Paging.Page,
            query.Paging.PageSize,
            ordered.Count);
    }
}
