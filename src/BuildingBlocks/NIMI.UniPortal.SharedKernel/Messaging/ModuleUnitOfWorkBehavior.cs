using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.EventBus;

namespace NIMI.UniPortal.SharedKernel.Messaging;

/// <summary>
/// One commit per command (solution-structure.md §6). Each module derives one of these for its own DbContext.
/// It only acts on <b>commands from its own module's assembly</b>: queries and other modules' commands pass
/// straight through, so modules never save each other's data.
/// <para>
/// Publish-after-commit (02-architecture.md §7.1): once the save succeeds it drains the scoped
/// <see cref="IEventCollector"/> and publishes each event. A failed handler or save discards the queue —
/// a command that did not commit publishes nothing. Both services are resolved lazily, so modules (and the
/// pipeline tests) that never registered the EventBus keep working unchanged.
/// </para>
/// </summary>
public abstract class ModuleUnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly bool IsCommand = typeof(TRequest).GetInterfaces()
        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));

    private readonly IServiceProvider _services;

    protected ModuleUnitOfWorkBehavior(IServiceProvider services) => _services = services;

    /// <summary>For module behaviors that resolve their own DbContext lazily inside <see cref="SaveChangesAsync"/>.</summary>
    protected IServiceProvider Services => _services;

    /// <summary>The module's Application assembly — the commands this behavior commits.</summary>
    protected abstract Assembly ModuleAssembly { get; }

    /// <summary>Exactly one SaveChangesAsync on the module's own context. Translate concurrency failures to <see cref="ConflictException"/>.</summary>
    protected abstract Task SaveChangesAsync(CancellationToken cancellationToken);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!IsCommand || typeof(TRequest).Assembly != ModuleAssembly)
            return await next();

        TResponse response;
        try
        {
            response = await next();          // the handler only mutates tracked entities
            await SaveChangesAsync(cancellationToken);
        }
        catch
        {
            _services.GetService<IEventCollector>()?.Drain();   // failed command: nothing may be published
            throw;
        }

        var collector = _services.GetService<IEventCollector>();
        var publisher = _services.GetService<IEventPublisher>();
        if (collector is null || publisher is null)
            return response;

        // Already committed: a client disconnect must not cancel the handlers — nothing can be rolled back now.
        foreach (var integrationEvent in collector.Drain())
            await publisher.PublishAsync(integrationEvent, CancellationToken.None);

        return response;
    }
}
