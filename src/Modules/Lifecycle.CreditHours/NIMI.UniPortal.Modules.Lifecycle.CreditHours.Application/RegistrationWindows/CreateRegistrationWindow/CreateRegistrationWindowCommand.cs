using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.RegistrationWindows.CreateRegistrationWindow;

/// <summary>P4.2 — opens the single registration window of a term (decision PREREQ).</summary>
public sealed record CreateRegistrationWindowCommand(
    Guid TermId,
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    int MinCredits,
    int MaxCredits) : ICommand<RegistrationWindowDto>;
