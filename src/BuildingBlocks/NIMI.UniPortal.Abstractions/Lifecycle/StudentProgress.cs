namespace NIMI.UniPortal.Abstractions.Lifecycle;

/// <summary>
/// A student's academic progress as the lifecycle variant reports it (P4.6). Earned credits and the cumulative GPA
/// come from published grades (P9; GPA null while no credit-bearing grade exists); graduation requirements are active
/// subjects not yet passed (MILEDU).
/// </summary>
public sealed record StudentProgress(
    Guid StudentId,
    int CreditsEarned,
    int CreditsInProgress,
    decimal? CumulativeGpa,
    IReadOnlyList<Guid> OutstandingRequiredSubjectIds);
