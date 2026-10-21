using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the CreditHours schema without knowing the module.</summary>
internal sealed class CreditHoursDatabase(CreditHoursDbContext db) : IModuleDatabase
{
    public string Module => "Lifecycle.CreditHours";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
