namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Progress;

/// <summary>
/// The same shape as the CreditHours progress (one frontend screen): credits of passed subjects, credits in progress
/// (the active term's enrollments without a result), the cumulative GPA over each subject's latest result (null
/// without credit-bearing grades) and the graduation requirements still outstanding.
/// </summary>
public sealed record StudentProgressDto(
    Guid StudentId,
    int CreditsEarned,
    int CreditsInProgress,
    decimal? CumulativeGpa,
    IReadOnlyList<OutstandingRequirementDto> OutstandingRequirements);
