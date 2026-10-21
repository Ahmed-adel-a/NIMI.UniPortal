namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Progress;

/// <summary>
/// P4.6 / P9.3b — credits earned (distinct passed subjects in the grade ledger), credits in progress (the active
/// term's registrations), the cumulative GPA (latest snapshot; null without credit-bearing grades) and the
/// graduation requirements still outstanding.
/// </summary>
public sealed record StudentProgressDto(
    Guid StudentId,
    int CreditsEarned,
    int CreditsInProgress,
    decimal? CumulativeGpa,
    IReadOnlyList<OutstandingRequirementDto> OutstandingRequirements);
