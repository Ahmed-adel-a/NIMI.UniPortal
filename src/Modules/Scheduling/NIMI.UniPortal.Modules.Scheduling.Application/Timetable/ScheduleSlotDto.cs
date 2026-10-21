using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;
using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable;

/// <summary>A timetable slot as the HTTP API shows it (P3.5); enums serialize as snake_case text.</summary>
public sealed record ScheduleSlotDto(
    Guid Id,
    Guid SubjectGroupId,
    string SubjectCode,
    int GroupNumber,
    Guid FacilityId,
    string FacilityName,
    Guid? StaffId,
    int DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Recurrence Recurrence,
    DateOnly ValidFrom,
    DateOnly ValidUntil,
    bool IsCancelled)
{
    public static ScheduleSlotDto From(ScheduleSlot slot, SubjectGroup group, Facility facility) =>
        new(
            slot.Id,
            slot.SubjectGroupId,
            group.Subject.Code,
            group.GroupNumber,
            slot.FacilityId,
            facility.Name,
            slot.StaffId,
            slot.DayOfWeek,
            slot.StartTime,
            slot.EndTime,
            slot.Recurrence,
            slot.ValidFrom,
            slot.ValidUntil,
            slot.IsCancelled);
}
