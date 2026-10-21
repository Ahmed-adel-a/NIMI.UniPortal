using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Grades.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the Grades schema without knowing the module.</summary>
internal sealed class GradesDatabase(GradesDbContext db) : IModuleDatabase
{
    public string Module => "Grades";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
