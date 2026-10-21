using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable.CreateScheduleSlot;

public sealed record CreateScheduleSlotCommand(
    Guid SubjectGroupId,
    Guid FacilityId,
    Guid? StaffId,
    int DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Recurrence Recurrence,
    DateOnly ValidFrom,
    DateOnly? ValidUntil,
    Guid CreatedBy) : ICommand<ScheduleSlotDto>;
