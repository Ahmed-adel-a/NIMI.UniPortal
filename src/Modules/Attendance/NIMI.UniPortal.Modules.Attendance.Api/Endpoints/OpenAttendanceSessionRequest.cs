namespace NIMI.UniPortal.Modules.Attendance.Api.Endpoints;

/// <summary>POST /api/attendance/sessions; <c>date</c> defaults to today in the effective timezone.</summary>
public sealed record OpenAttendanceSessionRequest(Guid SlotId, DateOnly? Date);
