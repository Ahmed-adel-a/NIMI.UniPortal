using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real Cairo host in memory against a throwaway PostgreSQL database.
/// Shared per test collection (see <see cref="PortalCollection"/>). Module migrations are applied
/// here once the first DbContext exists (P0.1), and data is reset between tests with Respawn
/// once there are tables to reset.
/// </summary>
public sealed class PortalWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly TestDatabase _database = new();

    public string ConnectionString => _database.ConnectionString;

    /// <summary>A client acting as the given role (student / professor / admin) via <see cref="TestAuthHandler"/>.</summary>
    public HttpClient CreateClientAs(string role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Portal", _database.ConnectionString);

        builder.ConfigureTestServices(services =>
            services.AddAuthentication(TestAuthHandler.SchemeName)
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
