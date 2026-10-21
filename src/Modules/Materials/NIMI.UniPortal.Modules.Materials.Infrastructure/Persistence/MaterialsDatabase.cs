using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Materials.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the Materials schema without knowing the module.</summary>
internal sealed class MaterialsDatabase(MaterialsDbContext db) : IModuleDatabase
{
    public string Module => "Materials";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
