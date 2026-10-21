namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.AtRisk;

/// <summary>One student below the threshold in one group.</summary>
public sealed record AtRiskItemDto(
    Guid StudentId,
    string FullName,
    string? StudentCode,
    Guid SubjectGroupId,
    string SubjectCode,
    int GroupNumber,
    decimal Percentage,
    decimal Threshold);
