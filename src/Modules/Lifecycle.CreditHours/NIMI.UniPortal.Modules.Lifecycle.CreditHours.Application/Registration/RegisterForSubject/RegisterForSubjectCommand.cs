using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.RegisterForSubject;

/// <summary>P4.3 — self-registration in the active term (decision D2: auto-approved, rules enforced).</summary>
public sealed record RegisterForSubjectCommand(
    Guid StudentId,
    bool IsStudent,
    Guid SubjectGroupId) : ICommand<RegistrationDto>;
