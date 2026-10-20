using System.Collections.Concurrent;

namespace NIMI.UniPortal.SharedKernel.Messaging;

/// <summary>Resolves the single handler for a request and runs it inside the registered behaviors.</summary>
internal sealed class Sender(IServiceProvider services) : ISender
{
    // One closed dispatcher per request type, built once — reflection only on first use.
    private static readonly ConcurrentDictionary<Type, object> Dispatchers = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispatcher = (ResponseDispatcher<TResponse>)Dispatchers.GetOrAdd(
            request.GetType(),
            static (requestType, responseType) => Activator.CreateInstance(
                typeof(RequestDispatcher<,>).MakeGenericType(requestType, responseType))!,
            typeof(TResponse));

        return dispatcher.Dispatch(request, services, cancellationToken);
    }
}
