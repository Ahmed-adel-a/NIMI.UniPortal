namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions;

/// <summary>One attendance session as the HTTP API shows it (P6.1).</summary>
public sealed record AttendanceSessionDto(
    Guid Id,
    Guid SlotId,
    Guid SubjectGroupId,
    string SubjectCode,
    int GroupNumber,
    DateOnly SessionDate,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    bool IsOpen);
