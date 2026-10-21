namespace Host.CairoUniversity.Dashboards;

/// <summary>One lecture occurrence on the student dashboard (wall-clock date and times in the faculty's timezone).</summary>
public sealed record DashboardLectureDto(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid SubjectId,
    string SubjectCode,
    string SubjectName,
    string SubjectNameAr,
    int GroupNumber,
    string FacilityName);
