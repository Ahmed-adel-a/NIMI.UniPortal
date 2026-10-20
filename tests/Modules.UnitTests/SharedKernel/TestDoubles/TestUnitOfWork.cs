using System.Reflection;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>A module unit of work whose "save" is just a trace entry — exercises the real base class.</summary>
public sealed class TestUnitOfWork<TRequest, TResponse>(SaveCounter saves, TestModule module)
    : ModuleUnitOfWorkBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    protected override Assembly ModuleAssembly => module.Assembly;

    protected override Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        saves.Trace.Add("saved");
        return Task.CompletedTask;
    }
}
