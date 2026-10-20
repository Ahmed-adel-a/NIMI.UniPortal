namespace NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

/// <summary>Base for tests that read or change data: every test starts from a freshly reset database.</summary>
[Collection(PortalCollection.Name)]
public abstract class PortalTest(PortalWebAppFactory factory) : IAsyncLifetime
{
    protected PortalWebAppFactory Factory { get; } = factory;

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
