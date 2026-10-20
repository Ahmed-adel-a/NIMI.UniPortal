namespace NIMI.UniPortal.SharedKernel.Messaging;

/// <summary>
/// What <see cref="Sender"/> knows at compile time: the response type only. The request type is closed
/// over by <see cref="RequestDispatcher{TRequest,TResponse}"/>, built once per request type.
/// </summary>
internal abstract class ResponseDispatcher<TResponse>
{
    public abstract Task<TResponse> Dispatch(IRequest<TResponse> request, IServiceProvider services, CancellationToken cancellationToken);
}
