using Microsoft.Extensions.DependencyInjection;

namespace NIMI.UniPortal.SharedKernel.Messaging;

/// <summary>Runs one request type: resolves its handler, wraps it in the registered behaviors, invokes the chain.</summary>
internal sealed class RequestDispatcher<TRequest, TResponse> : ResponseDispatcher<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> Dispatch(IRequest<TResponse> request, IServiceProvider services, CancellationToken cancellationToken)
    {
        var typed = (TRequest)request;
        var handler = services.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        RequestHandlerDelegate<TResponse> next = () => handler.Handle(typed, cancellationToken);

        // Registration order = outermost first, so wrap from the innermost outwards.
        foreach (var behavior in services.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse())
        {
            var inner = next;
            next = () => behavior.Handle(typed, inner, cancellationToken);
        }

        return next();
    }
}
