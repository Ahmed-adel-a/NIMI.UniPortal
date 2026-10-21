// ACCEPTANCE TEST — written by the consultant for phase P02. Do not modify; report problems instead.
using NIMI.UniPortal.EventBus;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

public sealed class FirstPingHandler(EventTrace trace) : IIntegrationEventHandler<PingHappened>
{
    public Task HandleAsync(PingHappened integrationEvent, CancellationToken cancellationToken)
    {
        trace.Entries.Add("first:" + integrationEvent.Name);
        return Task.CompletedTask;
    }
}
