namespace NIMI.UniPortal.Modules.Payments.Application.Abstractions;

/// <summary>What the application asks a gateway to start (P5.7).</summary>
public sealed record CheckoutRequest(
    Guid PaymentIntentId,
    decimal Amount,
    string Currency,
    string Description,
    Uri ReturnUrl);
