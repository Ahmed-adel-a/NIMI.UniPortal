using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog;

/// <summary>A catalog subject as the HTTP API shows it; the category serializes as snake_case text.</summary>
public sealed record SubjectDto(
    Guid Id,
    Guid DepartmentId,
    string Code,
    string Name,
    string NameAr,
    string? Description,
    int CreditHours,
    SubjectCategory Category,
    bool IsGraduationRequirement,
    bool IsActive)
{
    public static SubjectDto From(Subject subject) =>
        new(
            subject.Id,
            subject.DepartmentId,
            subject.Code,
            subject.Name,
            subject.NameAr,
            subject.Description,
            subject.CreditHours,
            subject.Category,
            subject.IsGraduationRequirement,
            subject.IsActive);
}
