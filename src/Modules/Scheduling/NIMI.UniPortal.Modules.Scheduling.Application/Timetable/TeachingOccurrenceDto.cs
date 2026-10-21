namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable;

/// <summary>One real teaching occurrence of a slot on a date (P3.6, rule R9). The API returns a plain array.</summary>
public sealed record TeachingOccurrenceDto(
    DateOnly Date,
    int DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid ScheduleId,
    string SubjectCode,
    string SubjectName,
    string SubjectNameAr,
    int GroupNumber,
    string FacilityName);
