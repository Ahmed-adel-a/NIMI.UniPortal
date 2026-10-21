// ACCEPTANCE TEST — written by the consultant for phase P02. Do not modify; report problems instead.
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>A command whose handler queues a <see cref="PingHappened"/>; FailInHandler makes it throw after queuing.</summary>
public sealed record QueuePing(string Name, bool FailInHandler = false) : ICommand<Unit>;
