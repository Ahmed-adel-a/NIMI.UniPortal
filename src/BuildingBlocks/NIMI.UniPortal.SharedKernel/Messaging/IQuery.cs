namespace NIMI.UniPortal.SharedKernel.Messaging;

/// <summary>Reads data, never saves. Handlers use AsNoTracking + projections.</summary>
public interface IQuery<TResponse> : IRequest<TResponse>;
