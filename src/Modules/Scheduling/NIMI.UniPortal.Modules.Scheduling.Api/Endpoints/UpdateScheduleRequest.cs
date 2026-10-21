using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>Body of <c>PUT /api/admin/schedules/{id}</c> — same checks, excluding the slot itself.</summary>
public sealed record UpdateScheduleRequest(
    Guid SubjectGroupId,
    Guid FacilityId,
    Guid? StaffId,
    int DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Recurrence Recurrence,
    DateOnly ValidFrom,
    DateOnly? ValidUntil);
