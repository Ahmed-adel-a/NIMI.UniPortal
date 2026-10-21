namespace NIMI.UniPortal.EventBus;

/// <summary>
/// Reacts to one integration event type. Handlers are scoped, run after the producer's commit and are
/// isolated: one failure is logged and the other handlers still run (02-architecture.md §7.1).
/// </summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
