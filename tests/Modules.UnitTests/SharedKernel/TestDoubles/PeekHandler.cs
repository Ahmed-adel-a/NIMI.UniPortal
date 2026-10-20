using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>Answers 42.</summary>
public sealed class PeekHandler : IRequestHandler<Peek, int>
{
    public Task<int> Handle(Peek request, CancellationToken cancellationToken) => Task.FromResult(42);
}
