using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Assignments.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the Assignments schema without knowing the module.</summary>
internal sealed class AssignmentsDatabase(AssignmentsDbContext db) : IModuleDatabase
{
    public string Module => "Assignments";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
