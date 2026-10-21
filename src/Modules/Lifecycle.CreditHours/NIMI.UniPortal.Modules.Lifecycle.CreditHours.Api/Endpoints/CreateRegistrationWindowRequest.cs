namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api.Endpoints;

/// <summary>
/// Body of <c>POST /api/admin/registration-windows</c>. The credit limits default to 12/21 (decision PREREQ) when
/// the admin omits them.
/// </summary>
public sealed record CreateRegistrationWindowRequest(
    Guid TermId,
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    int MinCredits = 12,
    int MaxCredits = 21);
