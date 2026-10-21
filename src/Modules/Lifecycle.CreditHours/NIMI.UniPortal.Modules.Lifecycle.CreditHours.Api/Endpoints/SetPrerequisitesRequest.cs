namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api.Endpoints;

/// <summary>Body of <c>PUT /api/admin/registration/prerequisites/{subjectId}</c> — the whole list, possibly empty.</summary>
public sealed record SetPrerequisitesRequest(IReadOnlyList<Guid>? PrerequisiteSubjectIds);
