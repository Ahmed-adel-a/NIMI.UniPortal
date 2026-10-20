using System.Reflection;

namespace NIMI.UniPortal.SharedKernel.Messaging;

/// <summary>
/// One commit per command (solution-structure.md §6). Each module derives one of these for its own DbContext.
/// It only acts on <b>commands from its own module's assembly</b>: queries and other modules' commands pass
/// straight through, so modules never save each other's data.
/// </summary>
public abstract class ModuleUnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly bool IsCommand = typeof(TRequest).GetInterfaces()
        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));

    /// <summary>The module's Application assembly — the commands this behavior commits.</summary>
    protected abstract Assembly ModuleAssembly { get; }

    /// <summary>Exactly one SaveChangesAsync on the module's own context. Translate concurrency failures to <see cref="ConflictException"/>.</summary>
    protected abstract Task SaveChangesAsync(CancellationToken cancellationToken);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!IsCommand || typeof(TRequest).Assembly != ModuleAssembly)
            return await next();

        var response = await next();          // the handler only mutates tracked entities
        await SaveChangesAsync(cancellationToken);
        return response;
    }
}
