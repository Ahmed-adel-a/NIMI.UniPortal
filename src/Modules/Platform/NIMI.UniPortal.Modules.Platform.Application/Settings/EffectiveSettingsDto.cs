using NIMI.UniPortal.Modules.Platform.Application.Settings.GetAdminSettings;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings;

/// <summary>The rules that actually apply to a faculty: university defaults with that faculty's overrides on top (P1.4).</summary>
public sealed record EffectiveSettingsDto(
    Guid? FacultyId,
    string TimeZone,
    int WeekStartDay,
    int[] WorkingDays,
    decimal AttendanceThreshold,
    int LateGraceMinutes,
    GradeBandDto[] GradingScale,
    string StudentCodeFormat,
    MilitaryEducationPolicyDto MilitaryEducationPolicy);
