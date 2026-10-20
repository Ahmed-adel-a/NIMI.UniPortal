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
/// Boots the real Cairo host in memory against a throwaway PostgreSQL database, with every module's
/// migrations applied on startup. Shared per test collection (see <see cref="PortalCollection"/>).
/// Tests that change data call <see cref="ResetDatabaseAsync"/> first (via <see cref="PortalTest"/>).
/// <para>
/// Authentication: requests carrying <see cref="TestAuthHandler.RoleHeader"/> use the test scheme (quick role
/// checks); everything else goes through the <b>real JWT validation</b> with tokens from POST /api/auth/login.
/// </para>
/// </summary>
public sealed class PortalWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestOrJwt = "TestOrJwt";

    private readonly TestDatabase _database = new();
    private readonly string _signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
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
    }
}
