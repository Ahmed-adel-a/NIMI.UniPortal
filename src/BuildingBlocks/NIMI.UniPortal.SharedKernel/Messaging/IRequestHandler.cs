namespace NIMI.UniPortal.SharedKernel.Messaging;

/// <summary>The single handler for a request type (registered by <see cref="MessagingServiceCollectionExtensions.AddRequestHandlers"/>).</summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
