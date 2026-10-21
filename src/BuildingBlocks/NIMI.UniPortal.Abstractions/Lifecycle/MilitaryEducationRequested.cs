using NIMI.UniPortal.EventBus;

namespace NIMI.UniPortal.Abstractions.Lifecycle;

/// <summary>
/// Published by the active lifecycle variant when a student registers or is assigned to a military-education group
/// (P5.5). Payments answers it by creating the student's fee (idempotent) and notifying them.
/// </summary>
public sealed record MilitaryEducationRequested(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid StudentId,
    Guid RegistrationId,
    Guid SubjectId,
    Guid TermId) : IIntegrationEvent;
