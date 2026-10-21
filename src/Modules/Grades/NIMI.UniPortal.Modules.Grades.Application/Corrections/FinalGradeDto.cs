using NIMI.UniPortal.Modules.Grades.Domain;

namespace NIMI.UniPortal.Modules.Grades.Application.Corrections;

/// <summary>One final grade as Student Affairs sees it after a correction.</summary>
public sealed record FinalGradeDto(
    Guid Id,
    Guid StudentId,
    Guid SubjectId,
    Guid SubjectGroupId,
    Guid TermId,
    int CreditHours,
    decimal Score,
    string Grade,
    decimal GradePoints,
    GradeStatus Status,
    DateTimeOffset? PublishedAt)
{
    public static FinalGradeDto From(FinalGrade g) =>
        new(g.Id, g.StudentId, g.SubjectId, g.SubjectGroupId, g.TermId, g.CreditHours, g.Score, g.Grade, g.GradePoints, g.Status, g.PublishedAt);
}
