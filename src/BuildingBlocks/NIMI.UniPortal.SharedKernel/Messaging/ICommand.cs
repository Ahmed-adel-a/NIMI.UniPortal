namespace NIMI.UniPortal.SharedKernel.Messaging;

/// <summary>Changes state. Committed once by the owning module's UnitOfWork behavior.</summary>
public interface ICommand<TResponse> : IRequest<TResponse>;
