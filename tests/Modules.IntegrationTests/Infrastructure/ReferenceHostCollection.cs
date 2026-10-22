namespace NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

/// <summary>Tests against the reference host share one host + one throwaway database, one at a time.</summary>
[CollectionDefinition(Name)]
public sealed class ReferenceHostCollection : ICollectionFixture<ReferenceHostFactory>
{
    public const string Name = "reference-host";
}
