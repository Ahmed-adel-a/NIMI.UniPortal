using NIMI.UniPortal.Modules.Platform.Application.Settings.GetAdminSettings;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetFacultySettings;

/// <summary>Only the rules this faculty overrides; null = inherited from the university defaults (P1.4).</summary>
public sealed record FacultySettingsOverridesDto(
    int? WeekStartDay,
    int[]? WorkingDays,
    decimal? AttendanceThreshold,
    int? LateGraceMinutes,
    GradeBandDto[]? GradingScale,
    string? StudentCodeFormat,
    MilitaryEducationPolicyDto? MilitaryEducationPolicy);
