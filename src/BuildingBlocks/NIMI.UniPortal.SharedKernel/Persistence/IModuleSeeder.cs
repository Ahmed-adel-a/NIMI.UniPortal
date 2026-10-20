namespace NIMI.UniPortal.SharedKernel.Persistence;

/// <summary>
/// Development-only starting data (e.g. the first admin account from env). The host runs every seeder after
/// migrations, in the Development environment only. Seeders must be idempotent.
/// </summary>
public interface IModuleSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
