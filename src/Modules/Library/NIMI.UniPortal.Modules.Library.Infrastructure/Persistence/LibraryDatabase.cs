using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Library.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the Library schema without knowing the module.</summary>
internal sealed class LibraryDatabase(LibraryDbContext db) : IModuleDatabase
{
    public string Module => "Library";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
