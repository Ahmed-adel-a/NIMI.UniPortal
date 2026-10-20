namespace NIMI.UniPortal.SharedKernel.Persistence;

public static class PortalDatabase
{
    /// <summary>One connection string per deployment (ConnectionStrings:Portal); every module uses its own schema in it.</summary>
    public const string ConnectionStringName = "Portal";
}
