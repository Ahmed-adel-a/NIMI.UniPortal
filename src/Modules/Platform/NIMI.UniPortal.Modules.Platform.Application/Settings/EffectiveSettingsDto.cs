using NIMI.UniPortal.Modules.Platform.Application.Settings.GetAdminSettings;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Platform.Domain.Settings;

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
    MilitaryEducationPolicyDto MilitaryEducationPolicy)
{
    /// <summary>Maps the cross-module contract shape to the HTTP shape (the mode becomes <c>{ "mode": "…" }</c>).</summary>
    public static EffectiveSettingsDto From(EffectiveSettings settings) =>
        new(settings.FacultyId,
            settings.TimeZone,
            settings.WeekStartDay,
            settings.WorkingDays,
            settings.AttendanceThreshold,
            settings.LateGraceMinutes,
            [.. settings.GradingScale.Select(b => new GradeBandDto(b.Grade, b.MinScore, b.Points))],
            settings.StudentCodeFormat,
            new MilitaryEducationPolicyDto(ParseMode(settings.MilitaryEducationMode)));

    /// <summary>Parses the contract's snake_case mode name back to the enum the HTTP DTO serializes.</summary>
    private static MilitaryEducationMode ParseMode(string mode) => mode switch
    {
        "opt_in" => MilitaryEducationMode.OptIn,
        "mandatory" => MilitaryEducationMode.Mandatory,
        _ => throw new InvalidOperationException($"Unknown military-education mode '{mode}'."),
    };
}
