using NIMI.UniPortal.EventBus;

namespace NIMI.UniPortal.Modules.Audit.Contracts;

/// <summary>
/// P10.2 — a producer queues this for a sensitive action; it is published after the producer's commit and the Audit
/// module stores one activity row per event (idempotent on <see cref="EventId"/>). <see cref="Action"/> and
/// <see cref="EntityType"/> come from <see cref="AuditActions"/> / <see cref="AuditEntities"/>; the summary is plain
/// text for people and never holds secrets.
/// </summary>
public sealed record AuditableActionPerformed(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid? ActorId,
    string Action,
    string EntityType,
    Guid EntityId,
    string Summary) : IIntegrationEvent;
