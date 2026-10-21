namespace NIMI.UniPortal.Modules.Scheduling.Contracts;

/// <summary>A catalog subject; <see cref="Category"/> is snake_case text (<c>regular</c>, <c>military_education</c>).</summary>
public sealed record SubjectRef(
    Guid Id,
    Guid DepartmentId,
    string Code,
    string Name,
    string NameAr,
    int CreditHours,
    string Category,
    bool IsGraduationRequirement,
    bool IsActive);
