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

public static class PortalDatabase
{
    /// <summary>One connection string per deployment (ConnectionStrings:Portal); every module uses its own schema in it.</summary>
    public const string ConnectionStringName = "Portal";
}
