using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace NIMI.UniPortal.SharedKernel.Messaging;

/// <summary>Resolves the single handler for a request and runs it inside the registered behaviors.</summary>
internal sealed class Sender(IServiceProvider services) : ISender
{
    // One closed dispatcher per request type, built once — reflection only on first use.
    private static readonly ConcurrentDictionary<Type, Dispatcher> Dispatchers = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispatcher = (Dispatcher<TResponse>)Dispatchers.GetOrAdd(
            request.GetType(),
            static (requestType, responseType) => (Dispatcher)Activator.CreateInstance(
                typeof(Dispatcher<,>).MakeGenericType(requestType, responseType))!,
            typeof(TResponse));

        return dispatcher.Dispatch(request, services, cancellationToken);
    }

    private abstract class Dispatcher;

    private abstract class Dispatcher<TResponse> : Dispatcher
    {
        public abstract Task<TResponse> Dispatch(IRequest<TResponse> request, IServiceProvider services, CancellationToken cancellationToken);
    }

    private sealed class Dispatcher<TRequest, TResponse> : Dispatcher<TResponse>
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
}
