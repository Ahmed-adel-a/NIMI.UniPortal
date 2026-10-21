namespace NIMI.UniPortal.EventBus;

/// <summary>Scoped in-memory queue — one per request scope, so requests never share events.</summary>
internal sealed class EventCollector : IEventCollector
{
    private readonly List<IIntegrationEvent> _events = [];

    public void Add(IIntegrationEvent integrationEvent) => _events.Add(integrationEvent);

    public IReadOnlyList<IIntegrationEvent> Drain()
    {
        if (_events.Count == 0)
            return [];

        var drained = _events.ToArray();
        _events.Clear();
        return drained;
    }
}
