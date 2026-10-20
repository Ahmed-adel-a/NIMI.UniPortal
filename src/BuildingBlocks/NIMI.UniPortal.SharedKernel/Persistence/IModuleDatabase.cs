namespace NIMI.UniPortal.SharedKernel.Persistence;

/// <summary>
/// A module's own database schema + migrations. Every module with a DbContext registers one,
/// so the host (and the integration tests) can migrate all modules without knowing them.
/// </summary>
public interface IModuleDatabase
{
    string Module { get; }

    Task MigrateAsync(CancellationToken cancellationToken);
}
