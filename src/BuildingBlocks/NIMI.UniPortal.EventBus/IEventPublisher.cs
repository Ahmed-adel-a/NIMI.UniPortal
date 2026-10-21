namespace NIMI.UniPortal.EventBus;

/// <summary>
/// Publishes an event to every registered handler of its runtime type, each handler in its own DI scope,
/// sequentially. A failing handler is logged and does not fail the publisher or the other handlers.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
