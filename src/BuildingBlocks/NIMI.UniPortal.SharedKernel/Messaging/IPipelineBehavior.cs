namespace NIMI.UniPortal.SharedKernel.Messaging;

/// <summary>Wraps the handler (outermost registered first). Each one is hidden control flow — justify it before adding.</summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
