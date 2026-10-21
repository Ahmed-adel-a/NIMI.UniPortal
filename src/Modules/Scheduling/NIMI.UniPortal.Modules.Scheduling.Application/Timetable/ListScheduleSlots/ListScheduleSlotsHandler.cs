using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable.ListScheduleSlots;

internal sealed class ListScheduleSlotsHandler(ISchedulingDbContext db)
    : IRequestHandler<ListScheduleSlotsQuery, PagedResult<ScheduleSlotDto>>
{
    public async Task<PagedResult<ScheduleSlotDto>> Handle(
        ListScheduleSlotsQuery query, CancellationToken cancellationToken)
    {
        var slots = db.ScheduleSlots.AsNoTracking()
            .Where(s => query.TermId == null || s.SubjectGroup.TermId == query.TermId)
            .Where(s => query.DepartmentId == null || s.SubjectGroup.Subject.DepartmentId == query.DepartmentId)
            .Where(s => query.FacilityId == null || s.FacilityId == query.FacilityId)
            .Where(s => query.StaffId == null || s.StaffId == query.StaffId)
            .Where(s => query.GroupId == null || s.SubjectGroupId == query.GroupId);

        var total = await slots.CountAsync(cancellationToken);
        var items = await slots
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ThenBy(s => s.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .Select(s => new ScheduleSlotDto(
                s.Id,
                s.SubjectGroupId,
                s.SubjectGroup.Subject.Code,
                s.SubjectGroup.GroupNumber,
                s.FacilityId,
                s.Facility.Name,
                s.StaffId,
                s.DayOfWeek,
                s.StartTime,
                s.EndTime,
                s.Recurrence,
                s.ValidFrom,
                s.ValidUntil,
                s.IsCancelled))
            .ToListAsync(cancellationToken);

        return new PagedResult<ScheduleSlotDto>(items, query.Paging.Page, query.Paging.PageSize, total);
    }
}
