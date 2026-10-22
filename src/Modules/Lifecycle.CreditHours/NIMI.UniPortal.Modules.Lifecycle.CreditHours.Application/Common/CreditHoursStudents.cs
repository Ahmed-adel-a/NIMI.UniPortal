using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Common;

/// <summary>
/// Decision LIFECYCLEDEPT: this variant serves only students whose department runs credit hours; year-group students
/// are placed by Student Affairs in the other variant.
/// </summary>
internal sealed class CreditHoursStudents(IUsersApi users, IPlatformSettingsApi platform)
{
    private readonly Dictionary<Guid, bool> byDepartment = [];

    public const string YearGroupsMessage =
        "Your department studies in year groups: Student Affairs places you in your subjects, there is no self-registration.";

    public async Task<bool> IsOnCreditHoursAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var placement = await users.GetStudentPlacementAsync(studentId, cancellationToken);
        return placement is null || await IsCreditHoursDepartmentAsync(placement.DepartmentId, cancellationToken);
    }

    public async Task<bool> IsCreditHoursDepartmentAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        if (!byDepartment.TryGetValue(departmentId, out var creditHours))
        {
            creditHours = await platform.GetAcademicSystemAsync(departmentId, cancellationToken) == AcademicSystems.CreditHours;
            byDepartment[departmentId] = creditHours;
        }

        return creditHours;
    }
}
