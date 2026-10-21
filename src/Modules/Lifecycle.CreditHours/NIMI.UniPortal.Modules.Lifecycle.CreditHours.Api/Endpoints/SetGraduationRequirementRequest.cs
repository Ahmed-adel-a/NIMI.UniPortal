namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api.Endpoints;

/// <summary>PUT /api/admin/graduation/requirements/{departmentId}.</summary>
public sealed record SetGraduationRequirementRequest(int RequiredCredits);
