// ACCEPTANCE TEST — written by the consultant for phase P04A. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Lifecycle.RegistrationSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>P4.2 — registration windows with per-window credit limits (decision PREREQ).</summary>
public sealed class RegistrationWindowTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Admin_opens_a_registration_window_for_a_term()
    {
        var termId = await CreateActiveTermAsync(Admin());
        var now = Factory.Clock.GetUtcNow();

        var response = await PostWindowAsync(Admin(), termId, now.AddHours(-1), now.AddDays(14), 12, 21);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var window = await BodyOf(response);
        Assert.Equal(termId, (Guid)window["termId"]!);
        Assert.Equal(12, (int)window["minCredits"]!);
        Assert.Equal(21, (int)window["maxCredits"]!);
        Assert.True((bool)window["isOpen"]!);
        var listed = (await Admin().GetFromJsonAsync<JsonObject>("/api/admin/registration-windows"))!;
        Assert.Equal(1, (int)listed["totalCount"]!);
    }

    [Fact]
    public async Task A_term_has_at_most_one_window()
    {
        var termId = await CreateActiveTermAsync(Admin());
        await OpenWindowAsync(Factory, Admin(), termId);
        var now = Factory.Clock.GetUtcNow();

        var second = await PostWindowAsync(Admin(), termId, now, now.AddDays(3));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Invalid_windows_are_a_400_naming_the_field()
    {
        var termId = await CreateActiveTermAsync(Admin());
        var now = Factory.Clock.GetUtcNow();

        var unknownTerm = await PostWindowAsync(Admin(), Guid.CreateVersion7(), now, now.AddDays(3));
        var closesFirst = await PostWindowAsync(Admin(), termId, now, now.AddDays(-1));
        var tooManyCredits = await PostWindowAsync(Admin(), termId, now, now.AddDays(3), 12, 41);

        Assert.Equal(HttpStatusCode.BadRequest, unknownTerm.StatusCode);
        Assert.Contains("termId", await FieldsOf(unknownTerm));
        Assert.Equal(HttpStatusCode.BadRequest, closesFirst.StatusCode);
        Assert.Contains("closesAt", await FieldsOf(closesFirst));
        Assert.Equal(HttpStatusCode.BadRequest, tooManyCredits.StatusCode);
        Assert.Contains("maxCredits", await FieldsOf(tooManyCredits));
    }

    [Fact]
    public async Task Admin_moves_a_window_and_its_open_state_follows_the_clock()
    {
        var termId = await CreateActiveTermAsync(Admin());
        var windowId = await OpenWindowAsync(Factory, Admin(), termId);
        var now = Factory.Clock.GetUtcNow();

        var moved = await PutWindowAsync(Admin(), windowId, now.AddDays(2), now.AddDays(9), 9, 18);

        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        var window = await BodyOf(moved);
        Assert.False((bool)window["isOpen"]!);
        Assert.Equal(9, (int)window["minCredits"]!);
        Assert.Equal(18, (int)window["maxCredits"]!);
        Assert.Equal(HttpStatusCode.NotFound, (await PutWindowAsync(Admin(), Guid.CreateVersion7(), now, now.AddDays(1))).StatusCode);
    }

    [Fact]
    public async Task Only_admins_manage_windows()
    {
        var termId = await CreateActiveTermAsync(Admin());
        var now = Factory.Clock.GetUtcNow();
        var body = new { termId, opensAt = now, closesAt = now.AddDays(3), minCredits = 12, maxCredits = 21 };

        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("student").PostAsJsonAsync("/api/admin/registration-windows", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Factory.CreateClientAs("professor").GetAsync("/api/admin/registration-windows")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Factory.CreateClient().GetAsync("/api/admin/registration-windows")).StatusCode);
    }

    private HttpClient Admin() => Factory.CreateClientAs("admin");
}
