using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetAdminSettings;

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
