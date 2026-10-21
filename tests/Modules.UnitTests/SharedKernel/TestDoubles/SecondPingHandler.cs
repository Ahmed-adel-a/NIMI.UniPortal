// ACCEPTANCE TEST — written by the consultant for phase P02. Do not modify; report problems instead.
using NIMI.UniPortal.EventBus;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

public sealed class SecondPingHandler(EventTrace trace) : IIntegrationEventHandler<PingHappened>
{
    public Task HandleAsync(PingHappened integrationEvent, CancellationToken cancellationToken)
    {
        trace.Entries.Add("second:" + integrationEvent.Name);
        return Task.CompletedTask;
    }
}
