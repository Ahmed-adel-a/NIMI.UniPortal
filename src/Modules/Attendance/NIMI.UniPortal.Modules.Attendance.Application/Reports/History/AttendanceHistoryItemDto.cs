namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.History;

/// <summary>One attendance record of the student.</summary>
public sealed record AttendanceHistoryItemDto(
    Guid SessionId,
    DateOnly SessionDate,
    string SubjectCode,
    string Status,
    string Method,
    DateTimeOffset? CheckedInAt);
