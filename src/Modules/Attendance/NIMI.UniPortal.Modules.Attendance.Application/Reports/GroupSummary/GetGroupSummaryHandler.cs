using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Common;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.GroupSummary;

internal sealed class GetGroupSummaryHandler(
    IAttendanceDbContext db,
    IAcademicLifecycle lifecycle,
    ISchedulingApi scheduling,
    IUsersApi users) : IRequestHandler<GetGroupSummaryQuery, PagedResult<GroupSummaryItemDto>>
{
    public async Task<PagedResult<GroupSummaryItemDto>> Handle(GetGroupSummaryQuery query, CancellationToken cancellationToken)
    {
        var group = await GroupAccess.EnsureCanSeeAsync(scheduling, query.GroupId, query.CallerId, query.IsAdmin, cancellationToken);

        var rates = await GroupRates.LoadAsync(db, group.Id, cancellationToken);
        var members = await GroupMembers.LoadAsync(lifecycle, users, group.Id, cancellationToken);

        var items = members
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .Select(m => new GroupSummaryItemDto(
                m.StudentId,
                m.FullName,
                m.StudentCode,
                rates.AttendedBy(m.StudentId),
                rates.Held,
                rates.PercentageOf(m.StudentId)))
            .ToList();

        return new PagedResult<GroupSummaryItemDto>(items, query.Paging.Page, query.Paging.PageSize, members.Count);
    }
}
