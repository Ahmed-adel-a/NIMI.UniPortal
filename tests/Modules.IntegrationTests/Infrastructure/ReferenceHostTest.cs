namespace NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

/// <summary>Base for tests against the reference host: every test starts from a freshly reset database.</summary>
[Collection(ReferenceHostCollection.Name)]
public abstract class ReferenceHostTest(ReferenceHostFactory factory) : IAsyncLifetime
{
    protected ReferenceHostFactory Factory { get; } = factory;

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
