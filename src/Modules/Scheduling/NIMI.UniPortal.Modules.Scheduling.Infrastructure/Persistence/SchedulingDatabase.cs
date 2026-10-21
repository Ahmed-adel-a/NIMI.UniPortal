using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Scheduling.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the Scheduling schema without knowing the module.</summary>
internal sealed class SchedulingDatabase(SchedulingDbContext db) : IModuleDatabase
{
    public string Module => "Scheduling";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
