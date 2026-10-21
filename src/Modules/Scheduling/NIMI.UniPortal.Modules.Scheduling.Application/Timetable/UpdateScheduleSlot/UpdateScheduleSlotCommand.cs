using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable.UpdateScheduleSlot;

public sealed record UpdateScheduleSlotCommand(
    Guid Id,
    Guid SubjectGroupId,
    Guid FacilityId,
    Guid? StaffId,
    int DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Recurrence Recurrence,
    DateOnly ValidFrom,
    DateOnly? ValidUntil) : ICommand<ScheduleSlotDto>;
