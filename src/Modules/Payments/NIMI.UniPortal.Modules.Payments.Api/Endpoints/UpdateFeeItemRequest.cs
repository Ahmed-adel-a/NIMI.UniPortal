namespace NIMI.UniPortal.Modules.Payments.Api.Endpoints;

/// <summary>Body of <c>PUT /api/admin/fee-items/{id}</c> (P5.1).</summary>
public sealed record UpdateFeeItemRequest(
    string Name,
    string NameAr,
    string? Description,
    decimal Amount,
    DateOnly DueOn,
    bool IsActive);
