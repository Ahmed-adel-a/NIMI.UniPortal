namespace NIMI.UniPortal.Modules.Payments.Application.Online.StartCheckout;

/// <summary>What the student's return page receives after starting an online checkout (P5.7).</summary>
public sealed record CheckoutDto(Guid PaymentIntentId, string RedirectUrl);
