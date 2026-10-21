namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.Export;

/// <summary>P10.4 — a group's closed sessions (oldest first) and every member's status in each, ready for a CSV.</summary>
public sealed record AttendanceSheetDto(
    Guid GroupId,
    string SubjectCode,
    int GroupNumber,
    IReadOnlyList<DateOnly> SessionDates,
    IReadOnlyList<AttendanceSheetRowDto> Rows);
