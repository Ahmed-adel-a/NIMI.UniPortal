using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Payments.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the Payments schema without knowing the module.</summary>
internal sealed class PaymentsDatabase(PaymentsDbContext db) : IModuleDatabase
{
    public string Module => "Payments";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
