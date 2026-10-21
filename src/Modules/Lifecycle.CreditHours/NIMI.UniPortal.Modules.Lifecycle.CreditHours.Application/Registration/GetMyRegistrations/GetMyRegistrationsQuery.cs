using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.GetMyRegistrations;

/// <summary>P4.3 — the student's own live registrations in a term (the active term by default).</summary>
public sealed record GetMyRegistrationsQuery(
    Guid StudentId,
    bool IsStudent,
    Guid? TermId) : IQuery<MyRegistrationsDto>;
