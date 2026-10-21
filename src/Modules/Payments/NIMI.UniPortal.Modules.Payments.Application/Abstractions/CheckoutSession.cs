namespace NIMI.UniPortal.Modules.Payments.Application.Abstractions;

/// <summary>The provider's checkout reference and the page the student is sent to.</summary>
public sealed record CheckoutSession(string ProviderReference, Uri RedirectUrl);
