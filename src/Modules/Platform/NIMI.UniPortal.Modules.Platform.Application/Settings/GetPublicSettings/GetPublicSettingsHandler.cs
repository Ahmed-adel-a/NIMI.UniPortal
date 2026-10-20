using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetPublicSettings;

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
