namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Progress;

/// <summary>
/// P4.6 — credits earned (grade ledger, empty until P9), credits in progress (the active term's registrations),
/// GPA (null until P9) and the graduation requirements still outstanding.
/// </summary>
public sealed record StudentProgressDto(
    Guid StudentId,
    int CreditsEarned,
    int CreditsInProgress,
    decimal? CumulativeGpa,
    IReadOnlyList<OutstandingRequirementDto> OutstandingRequirements);
