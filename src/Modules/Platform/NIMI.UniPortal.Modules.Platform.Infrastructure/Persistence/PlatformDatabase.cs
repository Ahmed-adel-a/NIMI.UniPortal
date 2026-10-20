using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Platform.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the Platform schema without knowing the module.</summary>
internal sealed class PlatformDatabase(PlatformDbContext db) : IModuleDatabase
{
    public string Module => "Platform";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
