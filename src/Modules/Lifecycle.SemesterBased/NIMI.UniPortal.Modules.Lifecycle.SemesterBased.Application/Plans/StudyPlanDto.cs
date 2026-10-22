namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Plans;

/// <summary>P4.7: a department's programme (its last level graduates), subjects ordered by level, term and code.</summary>
public sealed record StudyPlanDto(
    Guid DepartmentId, int Levels, IReadOnlyList<StudyPlanSubjectDto> Subjects, DateTimeOffset UpdatedAt);
