using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>Records "handled"; throws for the name "boom" so tests can check nothing is saved on failure.</summary>
public sealed class RenameHandler(SaveCounter saves) : IRequestHandler<Rename, Unit>
{
    public Task<Unit> Handle(Rename request, CancellationToken cancellationToken)
    {
        if (request.Name == "boom")
            throw new InvalidOperationException("handler failed");
        saves.Trace.Add("handled");
        return Task.FromResult(Unit.Value);
    }
}
