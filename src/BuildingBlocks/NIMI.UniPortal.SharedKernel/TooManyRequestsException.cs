namespace NIMI.UniPortal.SharedKernel;

/// <summary>The caller hit a rate limit (e.g. forum posts per minute). The host answers 429 <c>rate_limited</c>.</summary>
public sealed class TooManyRequestsException(string message) : Exception(message);
