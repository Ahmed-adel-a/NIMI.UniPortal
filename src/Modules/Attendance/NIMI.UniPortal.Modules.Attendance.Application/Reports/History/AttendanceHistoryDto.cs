namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.History;

/// <summary>A page of the student's records (newest session first) plus the rate per subject over closed sessions.</summary>
public sealed record AttendanceHistoryDto(
    IReadOnlyList<AttendanceHistoryItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<SubjectRateDto> Subjects);
