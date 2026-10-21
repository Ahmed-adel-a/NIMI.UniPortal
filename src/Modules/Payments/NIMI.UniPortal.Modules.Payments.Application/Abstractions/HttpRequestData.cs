namespace NIMI.UniPortal.Modules.Payments.Application.Abstractions;

/// <summary>The raw HTTP shape a gateway adapter needs; headers are case-insensitive for lookup.</summary>
public sealed record HttpRequestData(IReadOnlyDictionary<string, string> Headers, string Body);
