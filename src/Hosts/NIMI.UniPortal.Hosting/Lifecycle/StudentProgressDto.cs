namespace NIMI.UniPortal.Hosting.Lifecycle;

/// <summary>
/// The student's progress from their own academic system (decision LIFECYCLEDEPT): credits of passed subjects,
/// credits in progress this term, the cumulative GPA (null without credit-bearing grades) and the graduation
/// requirements still outstanding. Same shape for credit hours and year groups.
/// </summary>
public sealed record StudentProgressDto(
    Guid StudentId,
    string AcademicSystem,
    int CreditsEarned,
    int CreditsInProgress,
    decimal? CumulativeGpa,
    IReadOnlyList<OutstandingRequirementDto> OutstandingRequirements);
