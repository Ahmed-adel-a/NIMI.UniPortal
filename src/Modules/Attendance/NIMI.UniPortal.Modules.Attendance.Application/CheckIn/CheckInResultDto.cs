namespace NIMI.UniPortal.Modules.Attendance.Application.CheckIn;

/// <summary>The student's accepted scan (P6.2); status is <c>present</c> or <c>late</c>.</summary>
public sealed record CheckInResultDto(
    Guid SessionId,
    string Status,
    DateTimeOffset CheckedInAt,
    string SubjectCode);
