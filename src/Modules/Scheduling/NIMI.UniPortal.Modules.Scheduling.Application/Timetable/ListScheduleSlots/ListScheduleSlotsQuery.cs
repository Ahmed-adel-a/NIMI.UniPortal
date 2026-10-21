using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable.ListScheduleSlots;

public sealed record ListScheduleSlotsQuery(
    Guid? TermId,
    Guid? DepartmentId,
    Guid? FacilityId,
    Guid? StaffId,
    Guid? GroupId,
    PageRequest Paging) : IQuery<PagedResult<ScheduleSlotDto>>;
