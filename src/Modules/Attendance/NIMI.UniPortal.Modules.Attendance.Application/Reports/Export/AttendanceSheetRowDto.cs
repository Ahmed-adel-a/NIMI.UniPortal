namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.Export;

/// <summary>One member: totals over closed sessions and their status per session (aligned with the sheet's dates).</summary>
public sealed record AttendanceSheetRowDto(
    Guid StudentId,
    string FullName,
    string? StudentCode,
    int Attended,
    int Held,
    decimal Percentage,
    IReadOnlyList<string?> Statuses);
