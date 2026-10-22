using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Platform.Domain.Settings;
using NIMI.UniPortal.Modules.Platform.Infrastructure.Persistence;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.Modules.Users.Infrastructure.Persistence;
using Npgsql;
using Respawn;
using Respawn.Graph;

namespace NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

/// <summary>
/// Boots a real deployment host in memory against a throwaway PostgreSQL database, with every module's migrations
/// applied on startup. One subclass per host (<see cref="PortalWebAppFactory"/> = Cairo, <see cref="ReferenceHostFactory"/>
/// = the reference host with Library). Shared per test collection; tests that change data call
/// <see cref="ResetDatabaseAsync"/> first.
/// <para>
/// Authentication: requests carrying <see cref="TestAuthHandler.RoleHeader"/> use the test scheme (quick role
/// checks); everything else goes through the <b>real JWT validation</b> with tokens from POST /api/auth/login.
/// </para>
/// </summary>
/// <typeparam name="TEntryPoint">A type from the host assembly (its entry point).</typeparam>
public abstract class PortalHostFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>, IAsyncLifetime, IPortalHost
    where TEntryPoint : class
{
    private const string TestOrJwt = "TestOrJwt";

    private readonly TestDatabase _database = new();
    private readonly string _signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), "nimi-test-files-" + Guid.CreateVersion7().ToString("N"));
    private Respawner? _respawner;

    public string ConnectionString => _database.ConnectionString;

    /// <summary>The host's only clock. Push it forward to test time-based rules; reset before every test.</summary>
    public TestClock Clock { get; } = new();

    /// <summary>A client acting as the given role (student / professor / admin) via <see cref="TestAuthHandler"/>.</summary>
    public HttpClient CreateClientAs(string role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        return client;
    }

    /// <summary>A client sending a real bearer token.</summary>
    public HttpClient CreateClientWithToken(string accessToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>Creates an admin account straight in the database (no API involved).</summary>
    public async Task<Guid> CreateAdminAsync(string email, string password, bool mustChangePassword = false, bool isActive = true)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var admin = User.CreateAdmin("Test Admin", email, hasher.Hash(password), mustChangePassword, Clock.GetUtcNow());
        if (!isActive)
            admin.SetActive(false, Clock.GetUtcNow());

        db.Users.Add(admin);
        await db.SaveChangesAsync();
        return admin.Id;
    }

    /// <summary>Logs in through the API; returns the response body (accessToken, expiresAt, mustChangePassword).</summary>
    public async Task<JsonObject> LoginAsync(string email, string password)
    {
        var response = await CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
    }

    /// <summary>
    /// Publishes an integration event through the real EventBus, exactly as a producer's commit would
    /// (P2 test infrastructure — Notifications tests do not go through a producer module).
    /// </summary>
    public async Task PublishAsync(IIntegrationEvent integrationEvent)
    {
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IEventPublisher>()
            .PublishAsync(integrationEvent, CancellationToken.None);
    }

    /// <summary>
    /// Empties every module table (Respawn), keeps migration history, then restores the reference rows the
    /// migrations seed — today just the default settings row — so every test starts from a fresh deployment.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        _ = Services;   // make sure the host started, so all migrations ran
        Clock.Reset();

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
        builder.UseSetting("Auth:Jwt:SigningKey", _signingKey);
        // Uploads (P7) go to a throwaway folder, removed with the database at the end of the run.
        builder.UseSetting("Storage:RootPath", _storageRoot);
        // The React dev server's origin and the API docs, as a frontend developer's container has them.
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5173");
        builder.UseSetting("ApiDocs:Enabled", "true");
        // Emails stay in the outbox (no mail server); tests read reset links from it (decision ACCOUNTX).
        builder.UseSetting("Email:ExposeOutbox", "true");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

            // X-Test-Role header → test scheme; otherwise real JWT bearer (challenge 401 / forbid 403 follow suit).
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = TestOrJwt;
                    options.DefaultAuthenticateScheme = TestOrJwt;
                    options.DefaultChallengeScheme = TestOrJwt;
                    options.DefaultForbidScheme = TestOrJwt;
                })
                .AddPolicyScheme(TestOrJwt, TestOrJwt, options => options.ForwardDefaultSelector = context =>
                    context.Request.Headers.ContainsKey(TestAuthHandler.RoleHeader)
                        ? TestAuthHandler.SchemeName
                        : JwtBearerDefaults.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    async Task IAsyncLifetime.InitializeAsync() => await _database.CreateAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _database.DropAsync();
        if (Directory.Exists(_storageRoot))
            Directory.Delete(_storageRoot, recursive: true);
    }
}
