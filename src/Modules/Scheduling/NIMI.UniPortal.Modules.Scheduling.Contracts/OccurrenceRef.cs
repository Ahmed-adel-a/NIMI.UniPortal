namespace NIMI.UniPortal.Modules.Scheduling.Contracts;

/// <summary>One real occurrence of a timetable slot (P3.5): a date plus the slot's wall-clock time and subject.</summary>
public sealed record OccurrenceRef(
    DateOnly Date,
    int DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid SlotId,
    Guid GroupId,
    int GroupNumber,
    Guid SubjectId,
    string SubjectCode,
    string SubjectName,
    string SubjectNameAr,
    string FacilityName,
    Guid? StaffId);
