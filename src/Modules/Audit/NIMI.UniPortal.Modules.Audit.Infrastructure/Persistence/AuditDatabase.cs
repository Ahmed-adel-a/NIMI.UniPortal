using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Audit.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the Audit schema without knowing the module.</summary>
internal sealed class AuditDatabase(AuditDbContext db) : IModuleDatabase
{
    public string Module => "Audit";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
