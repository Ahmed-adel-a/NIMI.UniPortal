namespace NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

/// <summary>All integration tests share one host + one throwaway database, and run one at a time.</summary>
[CollectionDefinition(Name)]
public sealed class PortalCollection : ICollectionFixture<PortalWebAppFactory>
{
    public const string Name = "portal";
}
