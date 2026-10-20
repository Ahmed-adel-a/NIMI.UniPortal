using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>Answers without calling the rest of the pipeline (short-circuit).</summary>
public sealed class RefuseBehavior : IPipelineBehavior<Echo, string>
{
    public Task<string> Handle(Echo request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken) =>
        Task.FromResult("refused");
}
