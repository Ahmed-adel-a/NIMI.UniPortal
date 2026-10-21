namespace NIMI.UniPortal.Modules.Grades.Application.Mine;

/// <summary>One published grade of the signed-in student.</summary>
public sealed record MyGradeDto(
    Guid GradeId,
    Guid SubjectId,
    string SubjectCode,
    string SubjectName,
    string SubjectNameAr,
    int CreditHours,
    decimal Score,
    string Grade,
    decimal GradePoints);
