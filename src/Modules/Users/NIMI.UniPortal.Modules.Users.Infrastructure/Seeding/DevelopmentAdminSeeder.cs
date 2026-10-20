using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.Modules.Users.Infrastructure.Persistence;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.Users.Infrastructure.Seeding;

/// <summary>
/// Development only: creates the first admin from <c>SEED_ADMIN_EMAIL</c> / <c>SEED_ADMIN_PASSWORD</c> (env or user
/// secrets) when no admin exists yet. Without both values it does nothing. Production admins are provisioned
/// deliberately at go-live (phase DEP).
/// </summary>
internal sealed class DevelopmentAdminSeeder(UsersDbContext db, IPasswordHasher hasher, IConfiguration configuration, TimeProvider clock)
    : IModuleSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var email = configuration["SEED_ADMIN_EMAIL"];
        var password = configuration["SEED_ADMIN_PASSWORD"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin, cancellationToken))
            return;

        db.Users.Add(User.CreateAdmin("Administrator", email, hasher.Hash(password), mustChangePassword: false, clock.GetUtcNow()));
        await db.SaveChangesAsync(cancellationToken);   // a seeder is not a command: it saves its own work, once
    }
}
