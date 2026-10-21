using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>Body of <c>POST /api/admin/schedules</c> (P3.5).</summary>
public sealed record CreateScheduleRequest(
    Guid SubjectGroupId,
    Guid FacilityId,
    Guid? StaffId,
    int DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Recurrence Recurrence,
    DateOnly ValidFrom,
    DateOnly? ValidUntil);
