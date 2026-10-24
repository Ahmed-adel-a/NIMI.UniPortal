namespace NIMI.UniPortal.Hosting.Lifecycle;

/// <summary>A subject the student studies in the active term, with their group (either academic system).</summary>
public sealed record StudentCourseDto(
    Guid SubjectId,
    string SubjectCode,
    string SubjectName,
    string SubjectNameAr,
    int CreditHours,
    Guid GroupId,
    int GroupNumber);
