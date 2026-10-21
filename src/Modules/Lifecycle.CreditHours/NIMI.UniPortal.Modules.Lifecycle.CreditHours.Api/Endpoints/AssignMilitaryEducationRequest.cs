namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api.Endpoints;

/// <summary>Body of <c>POST /api/admin/military-education/assign</c> (P5.5).</summary>
public sealed record AssignMilitaryEducationRequest(
    IReadOnlyList<Guid>? StudentIds,
    Guid SubjectGroupId);
