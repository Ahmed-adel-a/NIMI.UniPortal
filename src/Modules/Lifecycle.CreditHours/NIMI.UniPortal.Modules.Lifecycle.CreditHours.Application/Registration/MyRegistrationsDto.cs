namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration;

/// <summary>The student's registrations and the window's limits (P4.3); <see cref="BelowMinimum"/> flags R6.</summary>
public sealed record MyRegistrationsDto(
    Guid TermId,
    IReadOnlyList<MyRegistrationDto> Registrations,
    int TotalCredits,
    int? MinCredits,
    int? MaxCredits,
    bool BelowMinimum);
