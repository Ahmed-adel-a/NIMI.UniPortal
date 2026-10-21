// ACCEPTANCE TEST — written by the consultant for phase P02. Do not modify; report problems instead.
using System.Reflection;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.UnitTests.SharedKernel.TestDoubles;

/// <summary>A module unit of work over the real base class; its "save" is a trace entry (or a failure).</summary>
public sealed class EventUnitOfWork<TRequest, TResponse>(IServiceProvider services, EventTrace trace)
    : ModuleUnitOfWorkBehavior<TRequest, TResponse>(services)
    where TRequest : IRequest<TResponse>
{
    protected override Assembly ModuleAssembly => typeof(EventUnitOfWork<,>).Assembly;

    protected override Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (trace.FailSave)
            throw new InvalidOperationException("save failed");
        trace.Entries.Add("saved");
        return Task.CompletedTask;
    }
}
