namespace NIMI.UniPortal.SharedKernel.Messaging;

/// <summary>The next step in the pipeline (another behavior, or finally the handler).</summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
