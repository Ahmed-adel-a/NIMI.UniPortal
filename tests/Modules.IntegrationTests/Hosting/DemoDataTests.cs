// ACCEPTANCE TEST — written by the consultant for phase P12 prep (frontend enablement). Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Host.CairoUniversity;
using NIMI.UniPortal.Hosting.DemoData;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Modules.IntegrationTests.Hosting;

/// <summary>
/// Decision DEVDOCKER: the frontend developer's container starts with a populated campus — faculties, a running
/// term with a timetable, professors and students who can log in with one demo password, registrations, fees,
/// posts and (Cairo) forum + events. Built through the real commands, so every business rule holds; runs once.
/// </summary>
public sealed class DemoDataTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string Password = "Demo@2026";

    [Fact]
    public async Task The_demo_campus_is_usable_through_the_api()
    {
        await SeedAsync();

        var today = DateOnly.FromDateTime(Factory.Clock.GetUtcNow().UtcDateTime);
        var twoWeeks = $"from={today:yyyy-MM-dd}&to={today.AddDays(13):yyyy-MM-dd}";

        var student = await TokenClientAsync(DemoCampus.StudentEmail);
        var schedule = await student.GetStringAsync($"/api/schedule?{twoWeeks}");
        Assert.Contains("CSE101", schedule);
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync("/api/dashboard")).StatusCode);
        var fees = await student.GetStringAsync("/api/payments/fees");
        Assert.Contains("tuition", fees);
        var feed = (await student.GetFromJsonAsync<JsonObject>("/api/announcements/feed"))!;
        Assert.NotEmpty(feed["items"]!.AsArray());

        var professor = await TokenClientAsync(DemoCampus.ProfessorEmail);
        Assert.Contains("CSE101", await professor.GetStringAsync($"/api/teaching/schedule?{twoWeeks}"));

        var admin = await TokenClientAsync(DemoCampus.AdminEmail);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/admin/dashboard/summary")).StatusCode);
        Assert.Contains("ENG", await admin.GetStringAsync("/api/faculties"));

        // Cairo sells Forum + Events: both have something to show.
        var categories = (await student.GetFromJsonAsync<JsonArray>("/api/forum/categories"))!;
        Assert.NotEmpty(categories);
        Assert.Contains("Career", await student.GetStringAsync("/api/events"));
    }

    [Fact]
    public async Task One_demo_student_still_has_to_change_the_password()
    {
        await SeedAsync();

        var login = await Factory.LoginAsync(DemoCampus.NewStudentEmail, Password);

        Assert.True((bool)login["mustChangePassword"]!);
    }

    [Fact]
    public async Task Seeding_twice_changes_nothing()
    {
        await SeedAsync();
        var admin = await TokenClientAsync(DemoCampus.AdminEmail);
        var before = await admin.GetStringAsync("/api/admin/users?pageSize=100");

        await SeedAsync();

        Assert.Equal(before, await admin.GetStringAsync("/api/admin/users?pageSize=100"));
    }

    [Fact]
    public async Task Disabled_demo_data_seeds_nothing()
    {
        await SeedAsync(enabled: false);

        var response = await Factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = DemoCampus.AdminEmail, password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Runs the shared seeder and Cairo's premium one in one scope, exactly as the Development host does.</summary>
    private async Task SeedAsync(bool enabled = true)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var options = new DemoDataOptions(enabled, Password);
        var campus = new DemoCampus();
        IModuleSeeder[] seeders =
        [
            ActivatorUtilities.CreateInstance<DemoCampusSeeder>(scope.ServiceProvider, options, campus),
            ActivatorUtilities.CreateInstance<CairoDemoSeeder>(scope.ServiceProvider, options, campus),
        ];
        foreach (var seeder in seeders)
            await seeder.SeedAsync(CancellationToken.None);
    }

    private async Task<HttpClient> TokenClientAsync(string email)
    {
        var login = await Factory.LoginAsync(email, Password);
        Assert.False((bool)login["mustChangePassword"]!);
        return Factory.CreateClientWithToken((string)login["accessToken"]!);
    }
}
