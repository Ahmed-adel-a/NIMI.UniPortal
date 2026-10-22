using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the SemesterBased schema without knowing the module.</summary>
internal sealed class SemesterDatabase(SemesterDbContext db) : IModuleDatabase
{
    public string Module => "Lifecycle.SemesterBased";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
