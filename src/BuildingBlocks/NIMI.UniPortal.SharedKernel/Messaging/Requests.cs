namespace NIMI.UniPortal.SharedKernel.Messaging;

// Our own minimal mediator (decision MEDIATOR — no MediatR). Deliberately tiny:
// one handler per request, plus the pipeline behaviors a module registers (Validation, UnitOfWork only).

/// <summary>A request with a response. Use <see cref="IQuery{TResponse}"/> or <see cref="ICommand{TResponse}"/>, not this directly.</summary>
public interface IRequest<TResponse>;

/// <summary>Reads data, never saves. Handlers use AsNoTracking + projections.</summary>
public interface IQuery<TResponse> : IRequest<TResponse>;

/// <summary>Changes state. Committed once by the owning module's UnitOfWork behavior.</summary>
public interface ICommand<TResponse> : IRequest<TResponse>;

/// <summary>For commands with nothing to return.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = default;
}

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>Wraps the handler (outermost registered first). Each one is hidden control flow — justify it before adding.</summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
