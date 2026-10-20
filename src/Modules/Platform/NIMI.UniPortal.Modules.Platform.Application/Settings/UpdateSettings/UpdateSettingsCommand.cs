using NIMI.UniPortal.Modules.Platform.Application.Settings.GetAdminSettings;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.UpdateSettings;

/// <summary>
/// PUT semantics: the admin form sends every setting plus the <see cref="Version"/> it loaded.
/// Committed by the Platform UnitOfWork behavior; a changed version → 409.
/// </summary>
public sealed record UpdateSettingsCommand(
    string UniversityName,
    string UniversityNameAr,
    string? Domain,
    string? LogoUrl,
    string PrimaryColor,
    string SecondaryColor,
    string TimeZone,
    int WeekStartDay,
    int[] WorkingDays,
    decimal AttendanceThreshold,
    int LateGraceMinutes,
    GradeBandDto[] GradingScale,
    string StudentCodeFormat,
    MilitaryEducationPolicyDto MilitaryEducationPolicy,
    uint Version) : ICommand<Unit>;
