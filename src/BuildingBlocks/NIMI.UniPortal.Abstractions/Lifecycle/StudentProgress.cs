namespace NIMI.UniPortal.Abstractions.Lifecycle;

/// <summary>
/// A student's academic progress as the lifecycle variant reports it (P4.6). GPA and earned credits stay empty
/// until grades are recorded (P9); graduation requirements are active subjects not yet passed (MILEDU).
/// </summary>
public sealed record StudentProgress(
    Guid StudentId,
    int CreditsEarned,
    int CreditsInProgress,
    decimal? CumulativeGpa,
    IReadOnlyList<Guid> OutstandingRequiredSubjectIds);
