using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Announcements.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the Announcements schema without knowing the module.</summary>
internal sealed class AnnouncementsDatabase(AnnouncementsDbContext db) : IModuleDatabase
{
    public string Module => "Announcements";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
