using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Platform.Domain.Settings;
using NIMI.UniPortal.Modules.Platform.Infrastructure.Persistence;
using Npgsql;
using Respawn;
using Respawn.Graph;

namespace NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real Cairo host in memory against a throwaway PostgreSQL database, with every module's
/// migrations applied on startup. Shared per test collection (see <see cref="PortalCollection"/>).
/// Tests that change data call <see cref="ResetDatabaseAsync"/> first (via <see cref="PortalTest"/>).
/// </summary>
public sealed class PortalWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly TestDatabase _database = new();
    private Respawner? _respawner;

    public string ConnectionString => _database.ConnectionString;

    /// <summary>A client acting as the given role (student / professor / admin) via <see cref="TestAuthHandler"/>.</summary>
    public HttpClient CreateClientAs(string role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        return client;
    }

    /// <summary>
    /// Empties every module table (Respawn), keeps migration history, then restores the reference rows the
    /// migrations seed — today just the default settings row — so every test starts from a fresh deployment.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        _ = Services;   // make sure the host started, so all migrations ran

        await using (var connection = new NpgsqlConnection(ConnectionString))
        {
            await connection.OpenAsync();
            _respawner ??= await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToExclude = ["public"],
                TablesToIgnore = [new Table("__ef_migrations_history")],
            });
            await _respawner.ResetAsync(connection);
        }

        await using var scope = Services.CreateAsyncScope();
        var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        platform.SystemSettings.Add(SystemSettings.CreateDefault(new DateTimeOffset(2026, 10, 19, 0, 0, 0, TimeSpan.Zero)));
        await platform.SaveChangesAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Portal", _database.ConnectionString);
        // Every module's migrations run when the host starts, against the throwaway database.
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");

        // The test scheme replaces JWT bearer for authenticate, challenge (401) and forbid (403).
        builder.ConfigureTestServices(services =>
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { }));
    }

    async Task IAsyncLifetime.InitializeAsync() => await _database.CreateAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _database.DropAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PortalCollection : ICollectionFixture<PortalWebAppFactory>
{
    public const string Name = "portal";
}

/// <summary>Base for tests that read or change data: every test starts from a freshly reset database.</summary>
[Collection(PortalCollection.Name)]
public abstract class PortalTest(PortalWebAppFactory factory) : IAsyncLifetime
{
    protected PortalWebAppFactory Factory { get; } = factory;

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
