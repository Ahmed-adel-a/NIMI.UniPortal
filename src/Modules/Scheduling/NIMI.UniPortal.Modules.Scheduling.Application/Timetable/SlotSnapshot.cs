using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable;

/// <summary>What a slot looked like at one moment — compared before/after an edit to decide whether anyone is told (P10.3).</summary>
internal sealed record SlotSnapshot(
    Guid GroupId,
    Guid FacilityId,
    string FacilityName,
    Guid? StaffId,
    int DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Recurrence Recurrence,
    DateOnly ValidFrom,
    DateOnly ValidUntil)
{
    public static SlotSnapshot Of(ScheduleSlot slot, string facilityName) =>
        new(slot.SubjectGroupId, slot.FacilityId, facilityName, slot.StaffId, slot.DayOfWeek, slot.StartTime, slot.EndTime,
            slot.Recurrence, slot.ValidFrom, slot.ValidUntil);
}
