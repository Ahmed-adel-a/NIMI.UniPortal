using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Events.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the Events schema without knowing the module.</summary>
internal sealed class EventsDatabase(EventsDbContext db) : IModuleDatabase
{
    public string Module => "Events";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
