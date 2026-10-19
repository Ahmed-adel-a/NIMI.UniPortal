using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetPublicSettings;

/// <summary>
/// The settings anyone may read before logging in (the login screen needs branding + the calendar basics).
/// Academic rules (thresholds, grading scale, code format, military-education policy) stay behind authentication.
/// </summary>
public sealed record GetPublicSettingsQuery : IQuery<PublicSettingsDto>;

public sealed record PublicSettingsDto(
    string UniversityName,
    string UniversityNameAr,
    string? LogoUrl,
    string PrimaryColor,
    string SecondaryColor,
    string TimeZone,
    int WeekStartDay,
    int[] WorkingDays);

internal sealed class GetPublicSettingsHandler(IPlatformDbContext db) : IRequestHandler<GetPublicSettingsQuery, PublicSettingsDto>
{
    public Task<PublicSettingsDto> Handle(GetPublicSettingsQuery query, CancellationToken cancellationToken) =>
        db.SystemSettings
            .AsNoTracking()
            .Select(s => new PublicSettingsDto(
                s.UniversityName,
                s.UniversityNameAr,
                s.LogoUrl,
                s.PrimaryColor,
                s.SecondaryColor,
                s.TimeZone,
                s.WeekStartDay,
                s.WorkingDays))
            .SingleAsync(cancellationToken);   // the row is seeded by the migration — missing = broken deployment
}
