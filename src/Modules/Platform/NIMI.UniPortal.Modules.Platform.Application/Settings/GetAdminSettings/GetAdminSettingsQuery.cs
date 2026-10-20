using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.Modules.Platform.Domain.Settings;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetAdminSettings;

/// <summary>Every setting, for the Student Affairs settings screen. Includes the version stamp needed to save.</summary>
public sealed record GetAdminSettingsQuery : IQuery<AdminSettingsDto>;

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

public sealed record GradeBandDto(string Grade, decimal MinScore, decimal Points);

public sealed record MilitaryEducationPolicyDto(MilitaryEducationMode Mode);

internal sealed class GetAdminSettingsHandler(IPlatformDbContext db) : IRequestHandler<GetAdminSettingsQuery, AdminSettingsDto>
{
    public async Task<AdminSettingsDto> Handle(GetAdminSettingsQuery query, CancellationToken cancellationToken)
    {
        var row = await db.SystemSettings
            .AsNoTracking()
            .Select(s => new { Settings = s, Version = EF.Property<uint>(s, SettingsVersion.PropertyName) })
            .SingleAsync(cancellationToken);

        var s = row.Settings;
        return new AdminSettingsDto(
            s.UniversityName, s.UniversityNameAr, s.Domain, s.LogoUrl, s.PrimaryColor, s.SecondaryColor,
            s.TimeZone, s.WeekStartDay, s.WorkingDays, s.AttendanceThreshold, s.LateGraceMinutes,
            [.. s.GradingScale.Select(b => new GradeBandDto(b.Grade, b.MinScore, b.Points))],
            s.StudentCodeFormat,
            new MilitaryEducationPolicyDto(s.MilitaryEducationPolicy.Mode),
            s.UpdatedAt,
            row.Version);
    }
}
