using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation.ResyncMilitaryEducation;

/// <summary>
/// P5.5 — republishes the fee request for every pending military-education registration of a term. Safe to repeat:
/// the Payments handler is idempotent on (fee item, student).
/// </summary>
public sealed record ResyncMilitaryEducationCommand(Guid TermId) : ICommand<MilitaryEducationResyncDto>;
