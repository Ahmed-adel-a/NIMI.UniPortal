namespace NIMI.UniPortal.Modules.Payments.Api.Endpoints;

/// <summary>Body of <c>POST /api/admin/payments</c> (P5.3). <c>PaidAt</c> defaults to the current instant.</summary>
public sealed record RecordPaymentRequest(
    Guid StudentId,
    Guid FeeItemId,
    decimal Amount,
    string Method,
    string? Reference,
    DateTimeOffset? PaidAt);
