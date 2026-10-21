namespace NIMI.UniPortal.Modules.Attendance.Application.Devices.List;

/// <summary>One request in the Student Affairs queue.</summary>
public sealed record DeviceChangeItemDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string NewFingerprint,
    string Reason,
    string Status,
    DateTimeOffset CreatedAt);
