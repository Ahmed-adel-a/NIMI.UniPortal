namespace NIMI.UniPortal.EventBus;

/// <summary>
/// Scoped queue of events raised by the current command. Nothing is published before the commit:
/// the module unit of work drains it only after <c>SaveChangesAsync</c> succeeds.
/// </summary>
public interface IEventCollector
{
    void Add(IIntegrationEvent integrationEvent);

    /// <summary>Returns the queued events in order and clears the queue (so a failure can discard them).</summary>
    IReadOnlyList<IIntegrationEvent> Drain();
}
