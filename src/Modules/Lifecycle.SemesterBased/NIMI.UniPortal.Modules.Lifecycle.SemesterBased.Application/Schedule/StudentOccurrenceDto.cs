namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Schedule;

/// <summary>
/// One real occurrence of a registered group's slot (P4.5). Same item shape as the teaching schedule, so the
/// frontend renders both with one component.
/// </summary>
public sealed record StudentOccurrenceDto(
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
