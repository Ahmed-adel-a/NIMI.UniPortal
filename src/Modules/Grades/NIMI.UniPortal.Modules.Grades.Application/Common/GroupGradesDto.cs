namespace NIMI.UniPortal.Modules.Grades.Application.Common;

/// <summary>P9.1 — a group's roster crossed with its grade rows.</summary>
public sealed record GroupGradesDto(
    Guid GroupId,
    int GroupNumber,
    Guid TermId,
    Guid SubjectId,
    string SubjectCode,
    string SubjectName,
    string SubjectNameAr,
    int CreditHours,
    IReadOnlyList<GroupGradeRowDto> Rows);
