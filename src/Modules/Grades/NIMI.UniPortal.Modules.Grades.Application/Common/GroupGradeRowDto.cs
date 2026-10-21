using NIMI.UniPortal.Modules.Grades.Domain;

namespace NIMI.UniPortal.Modules.Grades.Application.Common;

/// <summary>One roster student and their grade, if any (every grade field is null until the first draft).</summary>
public sealed record GroupGradeRowDto(
    Guid StudentId,
    string FullName,
    string? StudentCode,
    Guid? GradeId,
    decimal? Score,
    string? Grade,
    decimal? GradePoints,
    GradeStatus? Status);
