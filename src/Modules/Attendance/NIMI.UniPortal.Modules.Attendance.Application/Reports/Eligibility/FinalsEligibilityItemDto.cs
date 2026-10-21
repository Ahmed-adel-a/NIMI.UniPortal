namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.Eligibility;

/// <summary>One group member's finals standing: barred (<see cref="Eligible"/> false) below the faculty's threshold.</summary>
public sealed record FinalsEligibilityItemDto(
    Guid StudentId,
    string FullName,
    string? StudentCode,
    Guid GroupId,
    string SubjectCode,
    int GroupNumber,
    int Attended,
    int Held,
    decimal Percentage,
    decimal Threshold,
    bool Eligible);
