// ACCEPTANCE TEST — written by the consultant for phase P02. Do not modify; report problems instead.
using NIMI.UniPortal.EventBus;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>A minimal integration event for the EventBus tests.</summary>
public sealed record PingHappened(Guid EventId, DateTimeOffset OccurredAt, string Name) : IIntegrationEvent
{
    public static PingHappened Named(string name) => new(Guid.CreateVersion7(), DateTimeOffset.UnixEpoch, name);
}
