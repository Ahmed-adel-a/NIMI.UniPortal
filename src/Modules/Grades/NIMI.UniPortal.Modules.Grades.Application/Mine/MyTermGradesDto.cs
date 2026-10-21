namespace NIMI.UniPortal.Modules.Grades.Application.Mine;

/// <summary>One term's published grades and its GPA (null when the term's grades carry no credits).</summary>
public sealed record MyTermGradesDto(
    Guid TermId,
    int AcademicYear,
    string Number,
    decimal? TermGpa,
    IReadOnlyList<MyGradeDto> Grades);
