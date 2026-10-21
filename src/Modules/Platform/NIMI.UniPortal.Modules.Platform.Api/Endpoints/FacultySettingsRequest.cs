using NIMI.UniPortal.Modules.Platform.Application.Settings.GetAdminSettings;

namespace NIMI.UniPortal.Modules.Platform.Api.Endpoints;

/// <summary>
/// PUT /api/admin/faculties/{facultyId}/settings body (P1.4): the complete set of overrides, null = inherit.
/// <see cref="Version"/> is required only when the faculty already has overrides.
/// </summary>
public sealed record FacultySettingsRequest(
    int? WeekStartDay,
    int[]? WorkingDays,
    decimal? AttendanceThreshold,
    int? LateGraceMinutes,
    GradeBandDto[]? GradingScale,
    string? StudentCodeFormat,
    MilitaryEducationPolicyDto? MilitaryEducationPolicy,
    uint? Version);
