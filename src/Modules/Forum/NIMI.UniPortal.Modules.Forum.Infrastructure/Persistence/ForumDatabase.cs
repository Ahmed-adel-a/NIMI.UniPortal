using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Forum.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the Forum schema without knowing the module.</summary>
internal sealed class ForumDatabase(ForumDbContext db) : IModuleDatabase
{
    public string Module => "Forum";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
