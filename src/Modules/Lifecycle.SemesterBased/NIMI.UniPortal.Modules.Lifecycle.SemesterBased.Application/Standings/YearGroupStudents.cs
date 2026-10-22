using NIMI.UniPortal.Modules.Platform.Contracts;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

/// <summary>
/// Decision LIFECYCLEDEPT: this variant serves only departments that run year groups; credit-hours students register
/// themselves in the other variant. Answers are cached per department for the request.
/// </summary>
internal sealed class YearGroupStudents(IPlatformSettingsApi platform)
{
    private readonly Dictionary<Guid, bool> byDepartment = [];

    public async Task<bool> IsYearGroupDepartmentAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        if (!byDepartment.TryGetValue(departmentId, out var yearGroups))
        {
            yearGroups = await platform.GetAcademicSystemAsync(departmentId, cancellationToken) == AcademicSystems.YearGroups;
            byDepartment[departmentId] = yearGroups;
        }

        return yearGroups;
    }
}
