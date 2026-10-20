namespace NIMI.UniPortal.SharedKernel.Messaging;

// Our own minimal mediator (decision MEDIATOR — no MediatR). Deliberately tiny:
// one handler per request, plus the pipeline behaviors a module registers (Validation, UnitOfWork only).

/// <summary>A request with a response. Use <see cref="IQuery{TResponse}"/> or <see cref="ICommand{TResponse}"/>, not this directly.</summary>
public interface IRequest<TResponse>;
