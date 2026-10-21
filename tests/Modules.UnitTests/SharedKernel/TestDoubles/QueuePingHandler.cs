// ACCEPTANCE TEST — written by the consultant for phase P02. Do not modify; report problems instead.
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

public sealed class QueuePingHandler(IEventCollector events, EventTrace trace) : IRequestHandler<QueuePing, Unit>
{
    public Task<Unit> Handle(QueuePing request, CancellationToken cancellationToken)
    {
        events.Add(PingHappened.Named(request.Name));
        if (request.FailInHandler)
            throw new InvalidOperationException("handler failed after queuing");
        trace.Entries.Add("handled");
        return Task.FromResult(Unit.Value);
    }
}
