namespace NIMI.UniPortal.Modules.Platform.Domain.AcademicSystems;

/// <summary>
/// Decision LIFECYCLEDEPT: the department's own rule wins, then its faculty's, then the university's, then the
/// deployment's built-in default.
/// </summary>
public static class AcademicSystemRules
{
    public static (AcademicSystem System, AcademicSystemSource Source) Resolve(
        IEnumerable<AcademicSystemRule> rules, Guid? facultyId, Guid? departmentId, AcademicSystem fallback)
    {
        var list = rules as IReadOnlyCollection<AcademicSystemRule> ?? [.. rules];
        if (departmentId is { } department && list.FirstOrDefault(r => r.DepartmentId == department) is { } own)
            return (own.System, AcademicSystemSource.Department);
        if (facultyId is { } faculty && list.FirstOrDefault(r => r.FacultyId == faculty && r.DepartmentId is null) is { } ofFaculty)
            return (ofFaculty.System, AcademicSystemSource.Faculty);
        if (list.FirstOrDefault(r => r.FacultyId is null && r.DepartmentId is null) is { } university)
            return (university.System, AcademicSystemSource.University);
        return (fallback, AcademicSystemSource.Default);
    }
}
