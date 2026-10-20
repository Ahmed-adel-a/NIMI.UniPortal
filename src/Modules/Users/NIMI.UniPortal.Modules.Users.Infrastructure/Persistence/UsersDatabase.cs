using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Users.Infrastructure.Persistence;

/// <summary>Lets the host (and tests) migrate the Users schema without knowing the module.</summary>
internal sealed class UsersDatabase(UsersDbContext db) : IModuleDatabase
{
    public string Module => "Users";

    public Task MigrateAsync(CancellationToken cancellationToken) => db.Database.MigrateAsync(cancellationToken);
}
