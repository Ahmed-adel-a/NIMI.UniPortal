using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Notifications.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the Notifications schema without knowing the module.</summary>
internal sealed class NotificationsDatabase(NotificationsDbContext db) : IModuleDatabase
{
    public string Module => "Notifications";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
