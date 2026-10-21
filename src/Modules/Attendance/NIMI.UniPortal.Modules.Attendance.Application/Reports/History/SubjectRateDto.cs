namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.History;

/// <summary>The student's attendance rate in one subject (closed sessions only).</summary>
public sealed record SubjectRateDto(Guid SubjectId, string SubjectCode, int Attended, int Held, decimal Percentage);
