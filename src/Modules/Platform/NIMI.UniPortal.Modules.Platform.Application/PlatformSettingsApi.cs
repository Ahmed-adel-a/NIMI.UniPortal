using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.Modules.Platform.Contracts;

namespace NIMI.UniPortal.Modules.Platform.Application;

/// <summary>Platform's public read contract for other modules. Faculty overrides plug in here in P1.4.</summary>
internal sealed class PlatformSettingsApi(IPlatformDbContext db) : IPlatformSettingsApi
{
    public Task<string> GetStudentCodeFormatAsync(Guid facultyId, CancellationToken cancellationToken = default) =>
        db.SystemSettings.AsNoTracking().Select(s => s.StudentCodeFormat).SingleAsync(cancellationToken);
}
