namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.GroupSummary;

/// <summary>One student's attendance in the group (closed sessions only).</summary>
public sealed record GroupSummaryItemDto(
    Guid StudentId,
    string FullName,
    string? StudentCode,
    int Attended,
    int Held,
    decimal Percentage);
