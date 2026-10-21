namespace NIMI.UniPortal.Modules.Payments.Application.Abstractions;

/// <summary>A verified provider callback, reduced to the fields the application needs (P5.7).</summary>
public sealed record GatewayCallback(
    string ProviderReference,
    bool Succeeded,
    decimal Amount,
    string? FailureReason);
