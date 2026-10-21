using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;

namespace NIMI.UniPortal.Modules.Attendance.Application.Common;

/// <summary>Resolves the effective rules of the subject's faculty; nothing about the university is hardcoded.</summary>
internal static class AttendanceSettings
{
    public static async Task<EffectiveSettings> ForDepartmentAsync(
        IUsersApi users,
        IPlatformSettingsApi platform,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        var department = await users.GetDepartmentAsync(departmentId, cancellationToken)
            ?? throw new NotFoundException("The subject's department was not found.");
        return await platform.GetEffectiveSettingsAsync(department.FacultyId, cancellationToken);
    }

    public static TimeZoneInfo Zone(EffectiveSettings settings) =>
        TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZone);
}
