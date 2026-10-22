using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

/// <summary>The signed-in student's year level, status, subjects still owed and latest year-end decision.</summary>
public sealed record MyStandingDto(
    Guid StudentId,
    Guid DepartmentId,
    int Level,
    int FinalLevel,
    int YearsInLevel,
    StandingStatus Status,
    IReadOnlyList<CarriedSubjectDto> CarriedSubjects,
    LastDecisionDto? LastDecision);
