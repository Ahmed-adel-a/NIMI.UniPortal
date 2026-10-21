using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>Body of <c>POST /api/admin/subjects</c> (P3.2).</summary>
public sealed record CreateSubjectRequest(
    Guid DepartmentId,
    string Code,
    string Name,
    string NameAr,
    string? Description,
    int CreditHours,
    SubjectCategory Category,
    bool IsGraduationRequirement);
