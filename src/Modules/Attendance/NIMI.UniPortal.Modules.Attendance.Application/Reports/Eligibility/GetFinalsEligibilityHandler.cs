using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Common;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.Eligibility;

/// <summary>
/// P10.5 — rule R9 at the end of term: a member whose rate over the group's closed sessions is below the effective
/// threshold of the subject's faculty is barred from the final exam; everyone else is eligible. Groups without a closed
/// session are left out (nothing to judge yet). Ordered by subject code, group number, then student code.
/// </summary>
internal sealed class GetFinalsEligibilityHandler(
    IAttendanceDbContext db,
    IAcademicLifecycle lifecycle,
    ISchedulingApi scheduling,
    IUsersApi users,
    IPlatformSettingsApi platform) : IRequestHandler<GetFinalsEligibilityQuery, PagedResult<FinalsEligibilityItemDto>>
{
    public async Task<PagedResult<FinalsEligibilityItemDto>> Handle(GetFinalsEligibilityQuery query, CancellationToken cancellationToken)
    {
        if (!query.IsAdmin && !query.IsProfessor)
            throw new ForbiddenException("Only teaching staff and Student Affairs see finals eligibility.");

        var groups = await ReportGroups.LoadAsync(
            db, scheduling, query.CallerId, query.IsAdmin, query.TermId, query.GroupId, cancellationToken);

        var items = new List<FinalsEligibilityItemDto>();
        foreach (var group in groups)
        {
            var rates = await GroupRates.LoadAsync(db, group.Id, cancellationToken);
            if (rates.Held == 0)
                continue;

            var subject = await scheduling.GetSubjectAsync(group.SubjectId, cancellationToken);
            if (subject is null)
                continue;

            var settings = await AttendanceSettings.ForDepartmentAsync(users, platform, subject.DepartmentId, cancellationToken);
            if (query.FacultyId is { } facultyId && settings.FacultyId != facultyId)
                continue;

            var members = await GroupMembers.LoadAsync(lifecycle, users, group.Id, cancellationToken);
            items.AddRange(members
                .Select(m =>
                {
                    var percentage = rates.PercentageOf(m.StudentId);
                    return new FinalsEligibilityItemDto(
                        m.StudentId,
                        m.FullName,
                        m.StudentCode,
                        group.Id,
                        subject.Code,
                        group.GroupNumber,
                        rates.AttendedBy(m.StudentId),
                        rates.Held,
                        percentage,
                        settings.AttendanceThreshold,
                        percentage >= settings.AttendanceThreshold);
                })
                .Where(i => !query.BarredOnly || !i.Eligible));
        }

        var ordered = items
            .OrderBy(i => i.SubjectCode, StringComparer.Ordinal)
            .ThenBy(i => i.GroupNumber)
            .ThenBy(i => i.StudentCode, StringComparer.Ordinal)
            .ToList();

        return new PagedResult<FinalsEligibilityItemDto>(
            [.. ordered.Skip(query.Paging.Skip).Take(query.Paging.PageSize)],
            query.Paging.Page,
            query.Paging.PageSize,
            ordered.Count);
    }
}
