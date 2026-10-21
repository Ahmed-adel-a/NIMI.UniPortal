namespace NIMI.UniPortal.Modules.Payments.Api.Endpoints;

/// <summary>Body of the mock-only <c>POST /api/payments/mock/{id}/complete</c> (P5.7).</summary>
public sealed record CompleteMockPaymentRequest(string Outcome);
