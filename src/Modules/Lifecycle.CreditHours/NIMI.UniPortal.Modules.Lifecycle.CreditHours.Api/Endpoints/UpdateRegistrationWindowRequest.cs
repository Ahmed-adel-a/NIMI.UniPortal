namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api.Endpoints;

/// <summary>Body of <c>PUT /api/admin/registration-windows/{id}</c>; the term is immutable.</summary>
public sealed record UpdateRegistrationWindowRequest(
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    int MinCredits = 12,
    int MaxCredits = 21);
