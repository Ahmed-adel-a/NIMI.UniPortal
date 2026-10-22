namespace NIMI.UniPortal.Hosting.Errors;

/// <summary>The standard error envelope for every endpoint: <c>{ code, message, details[] }</c>.</summary>
public sealed record ApiError(string Code, string Message, IReadOnlyList<ApiErrorDetail> Details);
