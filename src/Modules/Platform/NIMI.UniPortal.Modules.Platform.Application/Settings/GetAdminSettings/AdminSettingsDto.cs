namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetAdminSettings;

/// <summary>All university-wide settings plus the <see cref="Version"/> to send back when saving.</summary>
public sealed record AdminSettingsDto(
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
    IReadOnlyList<GradeBandDto> GradingScale,
    string StudentCodeFormat,
    MilitaryEducationPolicyDto MilitaryEducationPolicy,
    DateTimeOffset? UpdatedAt,
    uint Version);
