namespace NIMI.UniPortal.EventBus;

/// <summary>
/// Something that already happened in one module and that other modules may react to
/// (02-architecture.md §7.1). Produced only after the producer's commit; delivery is in-process,
/// sequential and best-effort — a failing handler never fails the producer.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Unique per occurrence — consumers use it to stay idempotent.</summary>
    Guid EventId { get; }

    /// <summary>When it happened (UTC), taken from the producer's <see cref="TimeProvider"/>.</summary>
    DateTimeOffset OccurredAt { get; }
}
