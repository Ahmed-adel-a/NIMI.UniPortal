using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>Body of <c>PUT /api/admin/subjects/{id}</c> — the code is immutable (rule R3).</summary>
public sealed record UpdateSubjectRequest(
    string Name,
    string NameAr,
    string? Description,
    int CreditHours,
    SubjectCategory Category,
    bool IsGraduationRequirement,
    bool IsActive);
