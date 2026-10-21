using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.RegistrationWindows.UpdateRegistrationWindow;

/// <summary>P4.2 — moves a window or changes its credit limits; the term stays fixed.</summary>
public sealed record UpdateRegistrationWindowCommand(
    Guid Id,
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    int MinCredits,
    int MaxCredits) : ICommand<RegistrationWindowDto>;
