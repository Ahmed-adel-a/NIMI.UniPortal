// ACCEPTANCE TEST — written by the consultant for phase P02. Do not modify; report problems instead.
using NIMI.UniPortal.EventBus;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>Always fails — the publisher must log it and carry on.</summary>
public sealed class ThrowingPingHandler : IIntegrationEventHandler<PingHappened>
{
    public Task HandleAsync(PingHappened integrationEvent, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("handler exploded");
}
