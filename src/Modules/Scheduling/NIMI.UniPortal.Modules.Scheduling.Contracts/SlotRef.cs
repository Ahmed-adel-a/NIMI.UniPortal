namespace NIMI.UniPortal.Modules.Scheduling.Contracts;

/// <summary>A timetable slot; <see cref="Recurrence"/> is snake_case text and <see cref="ValidUntil"/> is never null.</summary>
public sealed record SlotRef(
    Guid Id,
    Guid GroupId,
    Guid FacilityId,
    string FacilityName,
    Guid? StaffId,
    int DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Recurrence,
    DateOnly ValidFrom,
    DateOnly? ValidUntil,
    bool IsCancelled);
