// ACCEPTANCE TEST — written by the consultant for decision NATIONALIDLOGIN (test accounts). Do not modify; report problems instead.
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
/// Development only: <c>Seed:TestAccount</c> (national ID + password, kept out of the repo) gives the owner one login
/// for every role: a student registered like the demo students, a professor teaching the Computer subjects, the
/// Computer department head and a Student Affairs account. The password is used as given, and no first-login change
/// is forced.
/// </summary>
public sealed class TestAccountsTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    private const string NationalId = "29901011234567";
    private const string Password = "1234567";   // weaker than the policy on purpose: test accounts take it as given

    [Fact]
    public async Task One_national_id_signs_in_as_every_role()
    {
        await SeedAsync(new TestAccountOptions(NationalId, Password));
        var today = DateOnly.FromDateTime(Factory.Clock.GetUtcNow().UtcDateTime);
        var twoWeeks = $"from={today:yyyy-MM-dd}&to={today.AddDays(13):yyyy-MM-dd}";

        var student = await SignInAsync("student");
        Assert.Contains("CSE101", await student.GetStringAsync($"/api/schedule?{twoWeeks}"));

        var professor = await SignInAsync("professor");
        Assert.Contains("CSE101", await professor.GetStringAsync($"/api/teaching/schedule?{twoWeeks}"));

        var head = await SignInAsync("department_head");
        Assert.Equal(HttpStatusCode.OK, (await head.GetAsync("/api/department/reports")).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await student.PostAsJsonAsync("/api/reports", new { category = "other", title = "Test", message = "Reaches my own head." })).StatusCode);
        var inbox = await head.GetStringAsync("/api/department/reports?status=open");
        Assert.Contains("Reaches my own head.", inbox);

        var admin = await SignInAsync("admin");
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/admin/users")).StatusCode);
    }

    [Fact]
    public async Task Seeding_twice_changes_nothing_and_no_settings_means_no_accounts()
    {
        await SeedAsync(new TestAccountOptions(null, null));
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(null)).StatusCode);

        await SeedAsync(new TestAccountOptions(NationalId, Password));
        await SeedAsync(new TestAccountOptions(NationalId, Password));

        var ask = await Login(null);
        Assert.Equal(HttpStatusCode.Conflict, ask.StatusCode);
        Assert.Equal(
            new[] { "admin", "department_head", "professor", "student" },
            (await ask.Content.ReadFromJsonAsync<JsonObject>())!["details"]!.AsArray().Select(d => (string)d!["error"]!).Order());
    }

    private async Task<HttpClient> SignInAsync(string role)
    {
        var login = await Login(role);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = (await login.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.False((bool)body["mustChangePassword"]!);
        return Factory.CreateClientWithToken((string)body["accessToken"]!);
    }

    private Task<HttpResponseMessage> Login(string? role) =>
        Factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = NationalId, password = Password, role });

    /// <summary>The demo campus first (as in Development), then the test accounts, in one scope.</summary>
    private async Task SeedAsync(TestAccountOptions testAccount)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var options = new DemoDataOptions(true, "Demo@2026");
        var campus = new DemoCampus();
        IModuleSeeder[] seeders =
        [
            ActivatorUtilities.CreateInstance<DemoCampusSeeder>(scope.ServiceProvider, options, campus),
            ActivatorUtilities.CreateInstance<CairoDemoSeeder>(scope.ServiceProvider, options, campus),
            ActivatorUtilities.CreateInstance<TestAccountsSeeder>(scope.ServiceProvider, testAccount),
        ];
        foreach (var seeder in seeders)
            await seeder.SeedAsync(CancellationToken.None);
    }
}
