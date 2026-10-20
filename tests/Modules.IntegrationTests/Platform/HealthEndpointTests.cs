using System.Net;
using Microsoft.AspNetCore.Hosting;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Platform;

[Collection(PortalCollection.Name)]
public sealed class HealthEndpointTests(PortalWebAppFactory factory)
{
    [Fact]
    public async Task Health_reports_the_process_alive()
    {
        var response = await factory.CreateClient().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_db_is_healthy_when_PostgreSQL_answers()
    {
        var response = await factory.CreateClient().GetAsync("/api/health/db");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_db_is_unhealthy_when_PostgreSQL_is_unreachable()
    {
        // Nothing listens on port 1 → connection refused, fast.
        await using var unreachable = factory.WithWebHostBuilder(b => b
            .UseSetting("ConnectionStrings:Portal", "Host=127.0.0.1;Port=1;Username=nobody;Password=none;Timeout=2")
            .UseSetting("Database:ApplyMigrationsOnStartup", "false"));   // can't migrate a database that isn't there

        var response = await unreachable.CreateClient().GetAsync("/api/health/db");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }
}
