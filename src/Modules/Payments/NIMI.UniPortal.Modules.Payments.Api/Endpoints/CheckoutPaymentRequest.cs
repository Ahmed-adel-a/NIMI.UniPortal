namespace NIMI.UniPortal.Modules.Payments.Api.Endpoints;

/// <summary>Body of <c>POST /api/payments/checkout</c> (P5.7).</summary>
public sealed record CheckoutPaymentRequest(Guid FeeItemId, decimal Amount);
